/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: JustinFyfe
 * Date: 2019-1-22
 */
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using NHapi.Model.V25.Datatype;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
using SanteDB.Core;
using SanteDB.Messaging.HL7.Configuration;
using SanteDB.Messaging.HL7.TransportProtocol;
using NHapi.Base.Util;
using NHapi.Base.Parser;
using NHapi.Model.V25.Message;
using SanteDB.Core.Security.Services;
using System.Security.Authentication;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security.Claims;
using SanteDB.Messaging.HL7.Utils;
using SanteDB.Core.Diagnostics;
using System.Diagnostics.Tracing;

namespace SanteDB.Messaging.HL7.Messages
{
    /// <summary>
    /// Represents a message handler
    /// </summary>
    public abstract class MessageHandlerBase : IHL7MessageHandler
    {

        // Configuration
        private Hl7ConfigurationSection m_configuration = ApplicationServiceContext.Current?.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();

        // Entry ASM hash
        private static String s_entryAsmHash = null;

        // Installation date
        private static DateTime? s_installDate = null;

        protected Tracer m_traceSource = new Tracer(Hl7Constants.TraceSourceName);

        /// <summary>
        /// Get the supported triggers
        /// </summary>
        public abstract string[] SupportedTriggers { get; }

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
        /// Handle the message generic handler
        /// </summary>
        /// <param name="e">The message event information</param>
        /// <returns>The result of the message handling</returns>
        public virtual IMessage HandleMessage(Hl7MessageReceivedEventArgs e)
        {
            try
            {
                this.Authenticate(e);
                if (!this.Validate(e.Message))
                    throw new ArgumentException("Invalid message");

                return this.HandleMessageInternal(e, MessageUtils.Parse(e.Message));
            }
            catch (Exception ex)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  "Error processing message: {0}", ex);
                return this.CreateNACK(typeof(ACK), e.Message, ex, e);
            }
        }

        /// <summary>
        /// Authetnicate
        /// </summary>
        private void Authenticate(Hl7MessageReceivedEventArgs e)
        {
            IPrincipal principal = null;
            var msh = e.Message.GetStructure("MSH") as MSH;
            var sft = e.Message.GetStructure("SFT") as SFT;
            var sessionService = ApplicationServiceContext.Current.GetService<ISessionProviderService>();

            if (String.IsNullOrEmpty(msh.Security.Value) && this.m_configuration.Security == SecurityMethod.Msh8)
                throw new SecurityException("Must carry MSH-8 authorization token information");
            if (msh.Security.Value?.StartsWith("sid://") == true) // Session identifier
            {
                var session = sessionService.Get(Enumerable.Range(5, msh.Security.Value.Length - 5)
                                    .Where(x => x % 2 == 0)
                                    .Select(x => Convert.ToByte(msh.Security.Value.Substring(x, 2), 16))
                                    .ToArray());
                principal = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>().Authenticate(session) as IClaimsPrincipal;
            }
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
                    deviceSecret = BitConverter.ToString(auth.AuthorizationToken).Replace("-",""),
                    applicationId = msh.SendingApplication.NamespaceID.Value,
                    applicationSecret = this.m_configuration.Security == SecurityMethod.Sft4 ? sft.SoftwareBinaryID.Value : // Authenticate app by SFT4
                        this.m_configuration.Security == SecurityMethod.Msh8 ? msh.Security.Value : // Authenticate app by MSH-8
                        BitConverter.ToString(auth.AuthorizationToken).Replace("-", ""); // Authenticate app using X509 certificate on the device

                IPrincipal devicePrincipal = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(deviceId, deviceSecret, AuthenticationMethod.Local),
                    applicationPrincipal = applicationSecret != null ? ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>()?.Authenticate(applicationId, applicationSecret) : null;

                if (applicationPrincipal == null && ApplicationServiceContext.Current.HostType == SanteDBHostType.Server)
                    throw new UnauthorizedAccessException("Server requires authenticated application");

                principal = new SanteDBClaimsPrincipal(new IIdentity[] { devicePrincipal.Identity, applicationPrincipal?.Identity }.OfType<IClaimsIdentity>());
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
                else if (this.m_configuration.Security == SecurityMethod.Msh8 && String.IsNullOrEmpty(msh.Security.Value))
                    throw new SecurityException("MSH-8 must be provided for authenticating application");

                String deviceId = $"{msh.SendingApplication.NamespaceID.Value}|{msh.SendingFacility.NamespaceID.Value}",
                   deviceSecret = msh.Security.Value,
                   applicationId = msh.SendingApplication.NamespaceID.Value,
                   applicationSecret = this.m_configuration.Security == SecurityMethod.Sft4 ? sft.SoftwareBinaryID.Value :
                                            this.m_configuration.Security == SecurityMethod.Msh8 ? msh.Security.Value : null;

                IPrincipal devicePrincipal = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(deviceId, deviceSecret, AuthenticationMethod.Local),
                    applicationPrincipal = applicationSecret != null ? ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>()?.Authenticate(applicationId, applicationSecret) : null;

                if (applicationPrincipal == null && ApplicationServiceContext.Current.HostType == SanteDBHostType.Server)
                    throw new UnauthorizedAccessException("Server requires authenticated application");
                principal = new SanteDBClaimsPrincipal(new IIdentity[] { devicePrincipal.Identity, applicationPrincipal?.Identity }.OfType<IClaimsIdentity>());
            }
            else 
                switch(this.m_configuration.AnonymousUser?.ToUpper())
                {
                    case "SYSTEM":
                        principal = AuthenticationContext.SystemPrincipal;
                        break;
                    case "ANONYMOUS":
                    default:
                        principal = AuthenticationContext.AnonymousPrincipal;
                        break;
                }
            // Pricipal
            if(principal != null)
                AuthenticationContext.Current = new AuthenticationContext(principal);

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
        protected virtual IMessage CreateNACK(Type nackType, IMessage request, Exception error, Hl7MessageReceivedEventArgs receiveData)
        {
            // Extract TIE into real cause
            while (error is TargetInvocationException)
                error = error.InnerException;

            IMessage retVal = null;
            if (error is DomainStateException)
                retVal = this.CreateACK(nackType, request, "AR", "Domain Error");
            else if (error is PolicyViolationException || error is SecurityException)
            {
                retVal = this.CreateACK(nackType, request, "AR", "Security Error");
                AuditUtil.AuditRestrictedFunction(error, receiveData.ReceiveEndpoint, receiveData.SolicitorEndpoint.ToString());
            }
            else if (error is AuthenticationException || error is UnauthorizedAccessException)
            {
                retVal = this.CreateACK(nackType, request, "AR", "Unauthorized");
                AuditUtil.AuditRestrictedFunction(error as AuthenticationException, receiveData.ReceiveEndpoint, receiveData.SolicitorEndpoint.ToString());
            }
            else if (error is Newtonsoft.Json.JsonException ||
                error is System.Xml.XmlException)
                retVal = this.CreateACK(nackType, request, "AR", "Messaging Error");
            else if (error is DuplicateNameException)
                retVal = this.CreateACK(nackType, request, "CR", "Duplicate Data");
            else if (error is FileNotFoundException || error is KeyNotFoundException)
                retVal = this.CreateACK(nackType, request, "CE", "Data not found");
            else if (error is DetectedIssueException)
                retVal = this.CreateACK(nackType, request, "CR", "Business Rule Violation");
            else if (error is DataPersistenceException)
            {
                // Data persistence failed because of D/I/E
                if(error.InnerException is DetectedIssueException)
                {
                    error = error.InnerException;
                    retVal = this.CreateACK(nackType, request, "CR", "Business Rule Violation");
                }
                else
                    retVal = this.CreateACK(nackType, request, "CE", "Error committing data");
            }
            else if (error is NotImplementedException)
                retVal = this.CreateACK(nackType, request, "AE", "Not Implemented");
            else if (error is NotSupportedException)
                retVal = this.CreateACK(nackType, request, "AR", "Not Supported");
            else
                retVal = this.CreateACK(nackType, request, "AE", "General Error");

           
            var msa = retVal.GetStructure("MSA") as MSA;
            msa.ErrorCondition.Identifier.Value = this.MapErrCode(error);
            msa.ErrorCondition.Text.Value = error.Message;

            int erc = 0;
            // Detected issue exception
            if (error is DetectedIssueException)
            {
                foreach (var itm in (error as DetectedIssueException).Issues)
                {
                    var err = retVal.GetStructure("ERR", erc++) as ERR;
                    err.HL7ErrorCode.Identifier.Value = "207";
                    err.Severity.Value = itm.Priority == Core.BusinessRules.DetectedIssuePriorityType.Error ? "E" : itm.Priority == Core.BusinessRules.DetectedIssuePriorityType.Warning ? "W" : "I";
                    err.GetErrorCodeAndLocation(err.ErrorCodeAndLocationRepetitionsUsed).CodeIdentifyingError.Text.Value = itm.Text;
                }
            }
            else
            {
                var ex = error.InnerException;
                while(ex != null)
                {
                    var err = retVal.GetStructure("ERR", erc++) as ERR;
                    err.HL7ErrorCode.Identifier.Value = this.MapErrCode(ex);
                    err.Severity.Value = "E";
                    err.GetErrorCodeAndLocation(err.ErrorCodeAndLocationRepetitionsUsed).CodeIdentifyingError.Text.Value = ex.Message;
                    ex = ex.InnerException;
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
        protected virtual IMessage CreateACK(Type ackType, IMessage request, String ackCode, String ackMessage)
        {
            var retVal = Activator.CreateInstance(ackType) as IMessage;
            (retVal.GetStructure("MSH") as MSH).SetDefault(request.GetStructure("MSH") as MSH);
            if((request.GetStructure("MSH") as MSH).VersionID.VersionID.Value == "2.5")
                (retVal.GetStructure("SFT") as SFT).SetDefault();
            var msa = retVal.GetStructure("MSA") as MSA;
            msa.MessageControlID.Value = (request.GetStructure("MSH") as MSH).MessageControlID.Value;
            msa.AcknowledgmentCode.Value = ackCode;
            msa.TextMessage.Value = ackMessage;

            // FAST ACK carry same response message type as request
            if (retVal is ACK)
            {
                (retVal as ACK).MSH.MessageType.MessageStructure.Value = (retVal as ACK).MSH.MessageType.MessageCode.Value = "ACK";
                (retVal as ACK).MSH.MessageType.TriggerEvent.Value = (request.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value;
                

            }

            return retVal;
        }

        
       
    }
}
