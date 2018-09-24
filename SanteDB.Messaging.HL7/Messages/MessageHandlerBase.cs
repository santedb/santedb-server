using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Messaging.HAPI;
using MARC.HI.EHRS.SVC.Messaging.HAPI.Configuration;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Model.V25.Message;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Segments;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Messages
{
    /// <summary>
    /// Represents a message handler
    /// </summary>
    public abstract class MessageHandlerBase : IHL7MessageHandler
    {

        // Configuration
        private HL7ConfigurationSection m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("marc.hi.ehrs.svc.messaging.hapi") as HL7ConfigurationSection;

        // Segment handlers
        private static Dictionary<String, ISegmentHandler> s_segmentHandlers = new Dictionary<string, ISegmentHandler>();

        // Entry ASM hash
        private static String s_entryAsmHash = null;

        // Installation date
        private static DateTime? s_installDate = null;

        /// <summary>
        /// Scan types for message handler
        /// </summary>
        static MessageHandlerBase()
        {
            foreach (var t in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).SelectMany(a => a.ExportedTypes.Where(t => typeof(ISegmentHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)))
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
        /// Gets the segment handler for the specified segment
        /// </summary>
        protected ISegmentHandler GetSegmentHandler(string name)
        {
            ISegmentHandler handler = null;
            s_segmentHandlers.TryGetValue(name, out handler);
            return handler;
        }

        /// <summary>
        /// Parses the specified message components
        /// </summary>
        /// <param name="message">The message to be parsed</param>
        /// <returns>The parsed message</returns>
        protected virtual Bundle Parse(IGroup message)
        {

            Bundle retVal = new Bundle();
            var finder = new SegmentFinder(message);
            while (finder.hasNextChild())
            {
                finder.nextChild();
                foreach (var current in finder.CurrentChildReps)
                {
                    if (current is AbstractGroupItem)
                        foreach (var s in (current as AbstractGroupItem)?.Structures.OfType<IGroup>())
                        {
                            var parsed = this.Parse(s);
                            retVal.Item.AddRange(parsed.Item.Select(i =>
                            {
                                var ret = i.Clone();
                                (ret as ITaggable)?.AddTag(".v2.group", current.GetStructureName());
                                return ret;
                            }));
                            retVal.ExpansionKeys.AddRange(parsed.ExpansionKeys);
                        }
                    else if (current is AbstractSegment)
                    {
                        // Empty, don't parse
                        if (PipeParser.Encode(current as AbstractSegment, new EncodingCharacters('|', "^~\\&")).Length == 3)
                            continue;
                        var handler = this.GetSegmentHandler(current.GetStructureName());
                        if (handler != null)
                        {
                            var parsed = handler.Parse(current as AbstractSegment, retVal.Item);
                            if (parsed.Any())
                            {
                                retVal.ExpansionKeys.Add(parsed.First().Key.GetValueOrDefault());
                                retVal.Item.AddRange(parsed.Select(i =>
                                {
                                    var ret = i.Clone();
                                    (ret as ITaggable)?.AddTag(".v2.segment", current.GetStructureName());
                                    return ret;
                                }));
                            }
                        }
                    }
                    else if (current is AbstractGroup)
                    {
                        var subObject = this.Parse(current as AbstractGroup);
                        retVal.Item.AddRange(subObject.Item.Select(i =>
                        {
                            var ret = i.Clone();
                            (ret as ITaggable)?.AddTag(".v2.group", current.GetStructureName());
                            return ret;
                        }));
                        retVal.ExpansionKeys.AddRange(subObject.ExpansionKeys);
                    }

                    // Tag the objects 
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
            ISession session = null;
            try
            {
                // Authenticated event, so we shall authenticate!
                session = this.Authenticate(e);

                if (!this.Validate(e.Message))
                    throw new ArgumentException("Invalid message");

                var retVal =  this.HandleMessageInternal(e, this.Parse(e.Message));
                if (session != null)
                    (retVal.GetStructure("MSH") as MSH).Security.Value = $"sid://{BitConverter.ToString(session.Id).Replace("-", "")}";
                return retVal;
            }
            catch (Exception ex)
            {
                var retVal = this.CreateNACK(e.Message, ex, e);
                if (session != null)
                    (retVal.GetStructure("MSH") as MSH).Security.Value = $"sid://{BitConverter.ToString(session.Id).Replace("-", "")}";
                return retVal;
            }
        }

        /// <summary>
        /// Authetnicate
        /// </summary>
        private ISession Authenticate(Hl7MessageReceivedEventArgs e)
        {
            ClaimsPrincipal principal = null;
            var msh = e.Message.GetStructure("MSH") as MSH;
            var sft = e.Message.GetStructure("SFT") as SFT;
            var sessionService = ApplicationContext.Current.GetService<ISessionProviderService>();

            // Authenticated args?
            if (msh.Security.Value.StartsWith("sid://")) // Session identifier
                return sessionService.Get(Enumerable.Range(5, msh.Security.Value.Length - 5)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(msh.Security.Value.Substring(x, 2), 16))
                                    .ToArray());
            else if (e is AuthenticatedHl7MessageReceivedEventArgs)
            {
                var auth = e as AuthenticatedHl7MessageReceivedEventArgs;

                // Ensure proper authentication exists
                if (String.IsNullOrEmpty(msh.SendingFacility.NamespaceID.Value))
                    throw new SecurityException("MSH-4 must be provided for authenticating device");
                else if (String.IsNullOrEmpty(msh.SendingApplication.NamespaceID.Value))
                    throw new SecurityException("MSH-3 must be provided for authenticating device/application");
                else if (this.m_configuration.Security == SecurityMethod.Sft4 && String.IsNullOrEmpty(sft.SoftwareBinaryID.Value))
                    throw new SecurityException("SFT-4 must be provided for authenticating application");
                else if (this.m_configuration.Security == SecurityMethod.Msh8 && String.IsNullOrEmpty(msh.Security.Value))
                    throw new SecurityException("MSH-8 must be provided for authenticating application");

                String deviceId = $"{msh.SendingApplication.NamespaceID.Value}|{msh.SendingFacility.NamespaceID.Value}",
                    deviceSecret = BitConverter.ToString(auth.AuthorizationToken).Replace("-", ""),
                    applicationId = msh.SendingApplication.NamespaceID.Value,
                    applicationSecret = this.m_configuration.Security == SecurityMethod.Sft4 ? sft.SoftwareBinaryID.Value :
                        this.m_configuration.Security == SecurityMethod.Msh8 ? msh.Security.Value : null;

                IPrincipal devicePrincipal = ApplicationContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(deviceId, deviceSecret),
                    applicationPrincipal = applicationSecret != null ? ApplicationContext.Current.GetService<IApplicationIdentityProviderService>().Authenticate(applicationId, applicationSecret) : null;
                principal = new ClaimsPrincipal(new IIdentity[] { devicePrincipal.Identity, applicationPrincipal?.Identity }.OfType<ClaimsIdentity>());
            }
            else if (this.m_configuration.Security != SecurityMethod.None)
            {
                // Ensure proper authentication exists
                if (String.IsNullOrEmpty(msh.SendingFacility.NamespaceID.Value) || String.IsNullOrEmpty(msh.Security.Value))
                    throw new SecurityException("MSH-4 and MSH-8 must always be provided for authenticating device when SLLP is not used");
                else if (String.IsNullOrEmpty(msh.SendingFacility.NamespaceID.Value))
                    throw new SecurityException("MSH-3 must be provided for authenticating application");
                else if (this.m_configuration.Security == SecurityMethod.Sft4 && String.IsNullOrEmpty(sft.SoftwareBinaryID.Value))
                    throw new SecurityException("SFT-4 must be provided for authenticating application");
                else if (this.m_configuration.Security == SecurityMethod.Msh8 && String.IsNullOrEmpty(sft.SoftwareBinaryID.Value))
                    throw new SecurityException("MSH-8 must be provided for authenticating application");

                String deviceId = $"{msh.SendingApplication.NamespaceID.Value}|{msh.SendingFacility.NamespaceID.Value}",
                   deviceSecret = msh.Security.Value,
                   applicationId = msh.SendingApplication.NamespaceID.Value,
                   applicationSecret = this.m_configuration.Security == SecurityMethod.Sft4 ? sft.SoftwareBinaryID.Value :
                                            this.m_configuration.Security == SecurityMethod.Msh8 ? msh.Security.Value : null;

                IPrincipal devicePrincipal = ApplicationContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(deviceId, deviceSecret),
                    applicationPrincipal = applicationSecret != null ? ApplicationContext.Current.GetService<IApplicationIdentityProviderService>().Authenticate(applicationId, applicationSecret) : null;

                principal = new ClaimsPrincipal(new IIdentity[] { devicePrincipal.Identity, applicationPrincipal?.Identity }.OfType<ClaimsIdentity>());
            }

            // Pricipal
            if(principal != null)
            {
                var session = sessionService.Establish(principal, DateTimeOffset.Now.AddMinutes(5), "v2");
                AuthenticationContext.Current = new AuthenticationContext(principal);
                return session;
            }

            return null;
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
            if (error is DetectedIssueException)
            {
                foreach (var itm in (error as DetectedIssueException).Issues)
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
            this.UpdateSFT(retVal.GetSFT());
            retVal.MSA.MessageControlID.Value = (request.GetStructure("MSH") as MSH).MessageControlID.Value;
            retVal.MSA.AcknowledgmentCode.Value = ackCode;
            retVal.MSA.TextMessage.Value = ackMessage;
            return retVal;
        }

        /// <summary>
        /// Add software information
        /// </summary>
        private void UpdateSFT(SFT sftSegment)
        {
            if (Assembly.GetEntryAssembly() == null) return;
            sftSegment.SoftwareVendorOrganization.OrganizationName.Value = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            sftSegment.SoftwareVendorOrganization.OrganizationNameTypeCode.Value = "D";
            sftSegment.SoftwareCertifiedVersionOrReleaseNumber.Value = Assembly.GetEntryAssembly().GetName().Version.ToString();
            sftSegment.SoftwareProductName.Value = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyProductAttribute>()?.Product;

            // SFT info
            if (!String.IsNullOrEmpty(Assembly.GetEntryAssembly().Location) && File.Exists(Assembly.GetEntryAssembly().Location))
            {
                if (s_entryAsmHash == null)
                {
                    using (var md5 = MD5.Create())
                    using (var stream = File.OpenRead(Assembly.GetEntryAssembly().Location))
                        s_entryAsmHash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
                }

                if (s_installDate == null)
                    s_installDate = new FileInfo(Assembly.GetEntryAssembly().Location).CreationTime;

                sftSegment.SoftwareBinaryID.Value = s_entryAsmHash;
                sftSegment.SoftwareInstallDate.Time.SetLongDate(s_installDate.Value);
            }

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
            msh.SendingFacility.NamespaceID.Value = config.JurisdictionData?.Name ?? System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;
            msh.ReceivingApplication.NamespaceID.Value = inbound.SendingApplication.NamespaceID.Value;
            msh.ReceivingFacility.NamespaceID.Value = inbound.SendingFacility.NamespaceID.Value;
            msh.DateTimeOfMessage.Time.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }
}
