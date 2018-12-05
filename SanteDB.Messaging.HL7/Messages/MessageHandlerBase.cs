﻿/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-9-25
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

        protected TraceSource m_traceSource = new TraceSource("SanteDB.Messaging.HL7");

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
                        var handler = SegmentHandlers.GetSegmentHandler(current.GetStructureName());
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
            try
            {
                this.Authenticate(e);
                if (!this.Validate(e.Message))
                    throw new ArgumentException("Invalid message");

                return this.HandleMessageInternal(e, this.Parse(e.Message));
            }
            catch (Exception ex)
            {
                return this.CreateNACK(typeof(ACK), e.Message, ex, e);
            }
        }

        /// <summary>
        /// Authetnicate
        /// </summary>
        private void Authenticate(Hl7MessageReceivedEventArgs e)
        {
            IClaimsPrincipal principal = null;
            var msh = e.Message.GetStructure("MSH") as MSH;
            var sft = e.Message.GetStructure("SFT") as SFT;
            var sessionService = ApplicationServiceContext.Current.GetService<ISessionProviderService>();

            // Authenticated args?
            if (String.IsNullOrEmpty(msh.Security.Value) && this.m_configuration.Security == SecurityMethod.Msh8)
                throw new SecurityException("Must carry MSH-8 authorization token information");
            if (msh.Security.Value.StartsWith("sid://")) // Session identifier
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
                    applicationSecret = this.m_configuration.Security == SecurityMethod.Sft4 ? sft.SoftwareBinaryID.Value :
                        this.m_configuration.Security == SecurityMethod.Msh8 ? msh.Security.Value : null;

                IPrincipal devicePrincipal = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(deviceId, deviceSecret),
                    applicationPrincipal = applicationSecret != null ? ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>()?.Authenticate(applicationId, applicationSecret) : null;

                if (applicationPrincipal == null && ApplicationServiceContext.HostType == SanteDBHostType.Server)
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

                IPrincipal devicePrincipal = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>().Authenticate(deviceId, deviceSecret),
                    applicationPrincipal = applicationSecret != null ? ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>()?.Authenticate(applicationId, applicationSecret) : null;

                if (applicationPrincipal == null && ApplicationServiceContext.HostType == SanteDBHostType.Server)
                    throw new UnauthorizedAccessException("Server requires authenticated application");
                principal = new SanteDBClaimsPrincipal(new IIdentity[] { devicePrincipal.Identity, applicationPrincipal?.Identity }.OfType<IClaimsIdentity>());
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
        protected IMessage CreateNACK(Type nackType, IMessage request, Exception error, Hl7MessageReceivedEventArgs receiveData)
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
                retVal = this.CreateACK(nackType, request, "CE", "Error committing data");
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
        protected IMessage CreateACK(Type ackType, IMessage request, String ackCode, String ackMessage)
        {
            var retVal = Activator.CreateInstance(ackType) as IMessage;
            this.UpdateMSH(retVal.GetStructure("MSH") as MSH, request.GetStructure("MSH") as MSH);
            this.UpdateSFT(retVal.GetStructure("SFT") as SFT);
            var msa = retVal.GetStructure("MSA") as MSA;
            msa.MessageControlID.Value = (request.GetStructure("MSH") as MSH).MessageControlID.Value;
            msa.AcknowledgmentCode.Value = ackCode;
            msa.TextMessage.Value = ackMessage;
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
                        s_entryAsmHash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-","");
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
            var config = this.m_configuration;
            msh.MessageControlID.Value = Guid.NewGuid().ToString();
            msh.SendingApplication.NamespaceID.Value = ApplicationServiceContext.Current.GetService<INetworkInformationService>()?.GetMachineName();
            msh.SendingFacility.UniversalID.Value = this.m_configuration.LocalFacility.ToString();
            msh.SendingFacility.UniversalIDType.Value = "GUID";
            msh.ReceivingApplication.NamespaceID.Value = inbound.SendingApplication.NamespaceID.Value;
            msh.ReceivingFacility.NamespaceID.Value = inbound.SendingFacility.NamespaceID.Value;
            msh.DateTimeOfMessage.Time.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
        }

       
    }
}
