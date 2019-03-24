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
using AtnaApi.Model;
using AtnaApi.Transport;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.Atna.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using SdbAudit = SanteDB.Core.Auditing;
using SanteDB.Core.Exceptions;

namespace SanteDB.Messaging.Atna
{
    /// <summary>
    /// Represents an audit service that communicates Audits via
    /// RFC3881 (ATNA style) audits
    /// </summary>
    [ServiceProvider("IHE ATNA Audit Dispatcher")]
    public class AtnaAuditService : IAuditDispatchService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "IHE ATNA Audit Dispatcher";

        /// <summary>
        /// Is the audit data running
        /// </summary>
        private bool m_isRunning = false;

        // Configuration
        protected AtnaConfigurationSection m_configuration;

        // Transporter
        private ITransporter m_transporter;

        /// <summary>
        /// Creates a new instance of the ATNA audit service
        /// </summary>
        public AtnaAuditService()
        {
            this.m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AtnaConfigurationSection>();
            
            switch(this.m_configuration.Transport)
            {
                case AtnaTransportType.File:
                    this.m_transporter = new FileSyslogTransport()
                    {
                        EndPoint = this.m_configuration.AuditTarget,
                        MessageFormat = this.m_configuration.Format
                    };
                    break;
                case AtnaTransportType.Stcp:
                    this.m_transporter = new STcpSyslogTransport()
                    {
                        ClientCertificate = this.m_configuration?.ClientCertificate?.Certificate,
                        EndPoint = this.m_configuration.AuditTarget,
                        MessageFormat = this.m_configuration.Format,
                        ServerCertificate = this.m_configuration?.ServerCertificate?.Certificate
                    };
                    break;
                case AtnaTransportType.Tcp:
                    this.m_transporter = new TcpSyslogTransport()
                    {
                        EndPoint = this.m_configuration.AuditTarget,
                        MessageFormat = this.m_configuration.Format,
                    };
                    break;
                case AtnaTransportType.Udp:
                    this.m_transporter = new UdpSyslogTransport()
                    {
                        EndPoint = this.m_configuration.AuditTarget,
                        MessageFormat = this.m_configuration.Format,
                    };
                    break;
                default:
                    throw new ConfigurationException($"Invalid transport type {this.m_configuration.Transport}");
            }


        }

        #region IAuditorService Members

        /// <summary>
        /// Queue the sending of an audit
        /// </summary>
        /// <param name="state"></param>
        private void SendAuditAsync(object state)
        {

            try
            {
                var ad = state as SdbAudit.AuditData;

                // Create the audit basic
                AuditMessage am = new AuditMessage(
                    ad.Timestamp, (ActionType)Enum.Parse(typeof(ActionType), ad.ActionCode.ToString()),
                    (OutcomeIndicator)Enum.Parse(typeof(OutcomeIndicator), ad.Outcome.ToString()),
                    (EventIdentifierType)Enum.Parse(typeof(EventIdentifierType), ad.EventIdentifier.ToString()),
                    null
                );
                if (ad.EventTypeCode != null)
                    am.EventIdentification.EventType.Add(new CodeValue<String>(ad.EventTypeCode.Code, ad.EventTypeCode.CodeSystem) { DisplayName = ad.EventTypeCode.DisplayName });

                am.SourceIdentification.Add(new AuditSourceIdentificationType()
                {
                    AuditEnterpriseSiteID = ad.Metadata.FirstOrDefault(o=>o.Key == SdbAudit.AuditMetadataKey.EnterpriseSiteID)?.Value ?? this.m_configuration.EnterpriseSiteId,
                    AuditSourceID = ad.Metadata.FirstOrDefault(o => o.Key == SdbAudit.AuditMetadataKey.AuditSourceID)?.Value ?? Dns.GetHostName(),
                    AuditSourceTypeCode = new List<CodeValue<AuditSourceType>>()
                    {
                        new CodeValue<AuditSourceType>(
                            (AuditSourceType)Enum.Parse(typeof(AuditSourceType), ad.Metadata.FirstOrDefault(o=>o.Key == SdbAudit.AuditMetadataKey.AuditSourceType)?.Value ?? "ApplicationServerProcess"))
                    }
                });
                
                // Add additional data like the participant
                bool thisFound = false;
                string dnsName = Dns.GetHostName();
                foreach (var adActor in ad.Actors)
                {
                    thisFound |= (adActor.NetworkAccessPointId == Environment.MachineName || adActor.NetworkAccessPointId == dnsName) &&
                        adActor.NetworkAccessPointType == SdbAudit.NetworkAccessPointType.MachineName;
                    var act = new AuditActorData()
                    {
                        NetworkAccessPointId = adActor.NetworkAccessPointId,
                        NetworkAccessPointType = (NetworkAccessPointType)Enum.Parse(typeof(NetworkAccessPointType), adActor.NetworkAccessPointType.ToString()),
                        NetworkAccessPointTypeSpecified = adActor.NetworkAccessPointType != 0,
                        UserIdentifier = adActor.UserIdentifier,
                        UserIsRequestor = adActor.UserIsRequestor,
                        UserName = adActor.UserName,
                        AlternativeUserId = adActor.AlternativeUserId
                    };
                    foreach (var rol in adActor.ActorRoleCode)
                        act.ActorRoleCode.Add(new CodeValue<string>(rol.Code, rol.CodeSystem)
                            {
                                DisplayName = rol.DisplayName
                            });
                    am.Actors.Add(act);
                }

                
                foreach (var aoPtctpt in ad.AuditableObjects)
                {
                    var atnaAo = new AuditableObject()
                    {
                        IDTypeCode = aoPtctpt.IDTypeCode.HasValue ?
                            aoPtctpt.IDTypeCode.Value != SdbAudit.AuditableObjectIdType.Custom ?
                                new CodeValue<AuditableObjectIdType>((AuditableObjectIdType)Enum.Parse(typeof(AuditableObjectIdType), aoPtctpt?.IDTypeCode?.ToString())) :
                                (aoPtctpt.CustomIdTypeCode != null ?
                                  new CodeValue<AuditableObjectIdType>()
                                  {
                                      Code = aoPtctpt.CustomIdTypeCode?.Code,
                                      CodeSystem = aoPtctpt.CustomIdTypeCode?.CodeSystem,
                                      DisplayName = aoPtctpt.CustomIdTypeCode?.DisplayName
                                  } : null) :
                            null,
                        LifecycleType = aoPtctpt.LifecycleType.HasValue ? (AuditableObjectLifecycle)Enum.Parse(typeof(AuditableObjectLifecycle), aoPtctpt.LifecycleType.ToString()) : 0,
                        LifecycleTypeSpecified = aoPtctpt.LifecycleType.HasValue,
                        ObjectId = aoPtctpt.ObjectId,
                        Role = aoPtctpt.Role.HasValue ? (AuditableObjectRole)Enum.Parse(typeof(AuditableObjectRole), aoPtctpt.Role.ToString()) : 0,
                        RoleSpecified = aoPtctpt.Role != 0,
                        Type = (AuditableObjectType)Enum.Parse(typeof(AuditableObjectType), aoPtctpt.Type.ToString()),
                        TypeSpecified = aoPtctpt.Type != 0,
                        ObjectSpec = aoPtctpt.QueryData ?? aoPtctpt.NameData,
                        ObjectSpecChoice = aoPtctpt.QueryData == null ? ObjectDataChoiceType.ParticipantObjectName : ObjectDataChoiceType.ParticipantObjectQuery
                    };
                    // TODO: Object Data
                    foreach(var kv in aoPtctpt.ObjectData)
                        if(!String.IsNullOrEmpty(kv.Key))
                            atnaAo.ObjectDetail.Add(new ObjectDetailType() {
                                Type = kv.Key,
                                Value = kv.Value
                            });
                    am.AuditableObjects.Add(atnaAo);
                }
                
                // Send the message
                this.m_transporter.SendMessage(am);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }

        /// <summary>
        /// Send an audit to the endpoint
        /// </summary>
        public void SendAudit(SdbAudit.AuditData ad)
        {
            ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem(SendAuditAsync, ad);
        }

        #endregion

    }
}
