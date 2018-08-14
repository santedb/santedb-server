using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Messaging.HAPI;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using NHapi.Base.Model;
using NHapi.Base.Util;
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Security.Audit;
using SanteDB.Messaging.HL7.Segments;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Messages
{
    /// <summary>
    /// Represents a message handler
    /// </summary>
    public abstract class MessageHandlerBase : IHL7MessageHandler
    {

        // Segment handlers
        private static Dictionary<String, ISegmentHandler> s_segmentHandlers = new Dictionary<string, ISegmentHandler>();

        /// <summary>
        /// Scan types for message handler
        /// </summary>
        static MessageHandlerBase()
        {
            foreach(var t in AppDomain.CurrentDomain.GetAssemblies().Where(a=>!a.IsDynamic).SelectMany(a => a.ExportedTypes.Where(t => typeof(ISegmentHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)))
            {
                var instance = Activator.CreateInstance(t) as ISegmentHandler;
                s_segmentHandlers.Add(instance.Name, instance);
            }
        }

        /// <summary>
        /// Allows overridden classes to implement the message handling logic
        /// </summary>
        /// <param name="e">The message receive event args</param>
        /// <returns>The resulting message</returns>
        protected abstract IMessage HandleMessageInternal(Hl7MessageReceivedEventArgs e, Bundle parsed);

        /// <summary>
        /// Validate the specified message
        /// </summary>
        /// <param name="message">The message to be validated</param>
        /// <returns>True if the message is valid</returns>
        protected abstract bool Validate(IMessage message);

        /// <summary>
        /// Parses the specified message components
        /// </summary>
        /// <param name="message">The message to be parsed</param>
        /// <returns>The parsed message</returns>
        protected virtual Bundle Parse(IGroup message)
        {
            
            Bundle retVal = new Bundle();
            var finder = new SegmentFinder(message);
            while(finder.hasNextChild())
            {
                finder.nextChild();
                foreach (var current in finder.CurrentChildReps)
                {
                    if (current is AbstractGroupItem)
                        foreach (var s in (current as AbstractGroupItem)?.Structures.OfType<IGroup>())
                        {
                            var parsed = this.Parse(s);
                            retVal.Item.AddRange(parsed.Item);
                            retVal.ExpansionKeys.AddRange(parsed.ExpansionKeys);
                        }
                    else if (current is AbstractSegment)
                    {
                        ISegmentHandler handler = null;
                        if (s_segmentHandlers.TryGetValue(current.GetStructureName(), out handler))
                        {
                            var parsed = handler.Parse(current as AbstractSegment, retVal.Item);
                            retVal.ExpansionKeys.Add(parsed.First().Key.GetValueOrDefault());
                            retVal.Item.AddRange(parsed);
                        }
                    }
                    else if (current is AbstractGroup)
                    {
                        var subObject = this.Parse(current as AbstractGroup);
                        retVal.Item.AddRange(subObject.Item);
                        retVal.ExpansionKeys.AddRange(subObject.ExpansionKeys);
                    }
                }
            }
            return retVal;
        }


        /// <summary>
        /// Handle the message generic handler
        /// </summary>
        /// <param name="e">The message event information</param>
        /// <returns>The result of the message handling</returns>
        public virtual IMessage HandleMessage(Hl7MessageReceivedEventArgs e)
        {
            try
            {
                if (!this.Validate(e.Message))
                    throw new ArgumentException("Invalid message");
                return this.HandleMessageInternal(e, this.Parse(e.Message));
            }
            catch(Exception ex)
            {
                return this.CreateNACK(e.Message, ex, e);
            }
        }

        /// <summary>
		/// Map detail to error code
		/// </summary>
		private string MapErrCode(Exception e)
        {
            string errCode = string.Empty;

            if (e is ConstraintException)
                errCode = "101";
            else if (e is DuplicateNameException)
                errCode = "205";
            else if (e is DataException || e is DetectedIssueException)
                errCode = "207";
            else if (e is VersionNotFoundException)
                errCode = "203";
            else if (e is NotImplementedException)
                errCode = "200";
            else if (e is KeyNotFoundException || e is FileNotFoundException)
                errCode = "204";
            else if (e is SecurityException)
                errCode = "901";
            else
                errCode = "207";
            return errCode;
        }

        /// <summary>
        /// Create a negative acknolwedgement from the specified exception
        /// </summary>
        /// <param name="request">The request message</param>
        /// <param name="error">The exception that occurred</param>
        /// <returns>NACK message</returns>
        protected IMessage CreateNACK(IMessage request, Exception error, Hl7MessageReceivedEventArgs receiveData)
        {
            // Extract TIE into real cause
            while (error is TargetInvocationException)
                error = error.InnerException; 

            ACK retVal = null;
            if (error is DomainStateException)
                retVal = this.CreateACK(request, "AR", "Domain Error");
            else if (error is PolicyViolationException || error is SecurityException)
            {
                retVal = this.CreateACK(request, "AR", "Security Error");
                AuditUtil.AuditRestrictedFunction(error, receiveData.ReceiveEndpoint);
            }
            else if (error is UnauthorizedRequestException || error is UnauthorizedAccessException)
            {
                retVal = this.CreateACK(request, "AR", "Unauthorized");
                AuditUtil.AuditRestrictedFunction(error as UnauthorizedRequestException, receiveData.ReceiveEndpoint);
            }
            else if (error is Newtonsoft.Json.JsonException ||
                error is System.Xml.XmlException)
                retVal = this.CreateACK(request, "AR", "Messaging Error");
            else if (error is DuplicateNameException)
                retVal = this.CreateACK(request, "CR", "Duplicate Data");
            else if (error is FileNotFoundException || error is KeyNotFoundException)
                retVal = this.CreateACK(request, "CE", "Data not found");
            else if (error is DetectedIssueException)
                retVal = this.CreateACK(request, "CR", "Business Rule Violation");
            else if (error is NotImplementedException)
                retVal = this.CreateACK(request, "AE", "Not Implemented");
            else if (error is NotSupportedException)
                retVal = this.CreateACK(request, "AR", "Not Supported");
            else
                retVal = this.CreateACK(request, "AE", "General Error");

            retVal.MSA.ErrorCondition.Identifier.Value = this.MapErrCode(error);
            retVal.MSA.ErrorCondition.Text.Value = error.Message;

            // Detected issue exception
            if(error is DetectedIssueException)
            {
                foreach(var itm in (error as DetectedIssueException).Issues)
                {
                    var err = retVal.GetERR(retVal.ERRRepetitionsUsed);
                    err.HL7ErrorCode.Identifier.Value = "207";
                    err.Severity.Value = itm.Priority == Core.Services.DetectedIssuePriorityType.Error ? "E" : itm.Priority == Core.Services.DetectedIssuePriorityType.Warning ? "W" : "I";
                    err.GetErrorCodeAndLocation(err.ErrorCodeAndLocationRepetitionsUsed).CodeIdentifyingError.Text.Value = itm.Text;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Create an acknowledge message
        /// </summary>
        /// <param name="request">The request which triggered this</param>
        /// <param name="ackCode">The acknowledgemode code</param>
        /// <param name="ackMessage">The message to append to the ACK</param>
        /// <returns></returns>
        protected ACK CreateACK(IMessage request, String ackCode, String ackMessage)
        {
            var retVal = new ACK();
            this.UpdateMSH(retVal.MSH, request.GetStructure("MSH") as MSH);
            retVal.MSA.MessageControlID.Value = (request.GetStructure("MSH") as MSH).MessageControlID.Value;
            retVal.MSA.AcknowledgmentCode.Value = ackCode;
            retVal.MSA.TextMessage.Value = ackMessage;
            return retVal;
        }

        /// <summary>
        /// Update the MSH on the specified MSH segment
        /// </summary>
        /// <param name="msh">The message header to be updated</param>
        /// <param name="inbound">The inbound message</param>
        protected void UpdateMSH(MSH msh, MSH inbound)
        {
            var config = ApplicationContext.Current.Configuration;
            msh.MessageControlID.Value = Guid.NewGuid().ToString();
            msh.SendingApplication.NamespaceID.Value = config.DeviceName ?? Environment.MachineName;
            msh.SendingFacility.NamespaceID.Value = config.JurisdictionData.Name;
            msh.ReceivingApplication.NamespaceID.Value = inbound.SendingApplication.NamespaceID.Value;
            msh.ReceivingFacility.NamespaceID.Value = inbound.ReceivingFacility.NamespaceID.Value;
            msh.DateTimeOfMessage.Time.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }
}
