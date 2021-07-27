/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using AtnaApi.Model;
using AtnaApi.Transport;
using SanteDB.Core;
using SanteDB.Core.Model;
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
using SanteDB.Core.Auditing;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Diagnostics;

namespace SanteDB.Messaging.Atna
{
    /// <summary>
    /// Represents an audit service that communicates Audits via
    /// RFC3881 (ATNA style) audits
    /// </summary>
    [ServiceProvider("IHE ATNA Audit Dispatcher")]
    public class AtnaAuditService : IAuditDispatchService
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AtnaAuditService));

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
                    throw new InvalidOperationException($"Invalid transport type {this.m_configuration.Transport}");
            }


        }

        #region IAuditorService Members

        /// <summary>
        /// Queue the sending of an audit
        /// </summary>
        /// <param name="state"></param>
        private void SendAuditAsync(object state)
        {

            using (AuthenticationContext.EnterSystemContext())
            {
                try
                {
                    var ad = state as SdbAudit.AuditData;

                    // Translate codes to DICOM
                    if (ad.EventTypeCode != null)
                    {
                        IConceptRepositoryService icpcr = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();
                        var concept = icpcr?.GetConcept(ad.EventTypeCode.Code);
                        if (concept != null)
                        {
                            var refTerm = icpcr.GetConceptReferenceTerm(concept.Key.Value, "DCM");
                            if (refTerm != null)
                                ad.EventTypeCode = new AuditCode(refTerm.Mnemonic, "DCM") { DisplayName = refTerm.LoadCollection<ReferenceTermName>("DisplayNames")?.FirstOrDefault()?.Name };
                            else
                                ad.EventTypeCode.DisplayName = concept.LoadCollection<ConceptName>("ConceptNames").FirstOrDefault()?.Name;
                        }
                        this.m_tracer.TraceVerbose("Mapped Audit Type Code - {0}-{1}-{2}", ad.EventTypeCode.CodeSystem, ad.EventTypeCode.Code, ad.EventTypeCode.DisplayName);

                    }

                    // Create the audit basic
                    AuditMessage am = new AuditMessage(
                        ad.Timestamp, (AtnaApi.Model.ActionType)Enum.Parse(typeof(AtnaApi.Model.ActionType), ad.ActionCode.ToString()),
                        (AtnaApi.Model.OutcomeIndicator)Enum.Parse(typeof(AtnaApi.Model.OutcomeIndicator), ad.Outcome.ToString()),
                        (AtnaApi.Model.EventIdentifierType)Enum.Parse(typeof(AtnaApi.Model.EventIdentifierType), ad.EventIdentifier.ToString()),
                        null
                    );
                    if (ad.EventTypeCode != null)
                        am.EventIdentification.EventType.Add(new CodeValue<String>(ad.EventTypeCode.Code, ad.EventTypeCode.CodeSystem) { DisplayName = ad.EventTypeCode.DisplayName });

                    am.SourceIdentification.Add(new AuditSourceIdentificationType()
                    {
                        AuditEnterpriseSiteID = ad.Metadata.FirstOrDefault(o => o.Key == SdbAudit.AuditMetadataKey.EnterpriseSiteID)?.Value ?? this.m_configuration.EnterpriseSiteId,
                        AuditSourceID = ad.Metadata.FirstOrDefault(o => o.Key == SdbAudit.AuditMetadataKey.AuditSourceID)?.Value ?? Dns.GetHostName(),
                        AuditSourceTypeCode = new List<CodeValue<AtnaApi.Model.AuditSourceType>>()
                    {
                        new CodeValue<AtnaApi.Model.AuditSourceType>(
                            (AtnaApi.Model.AuditSourceType)Enum.Parse(typeof(AtnaApi.Model.AuditSourceType), ad.Metadata.FirstOrDefault(o=>o.Key == SdbAudit.AuditMetadataKey.AuditSourceType)?.Value ?? "ApplicationServerProcess"))
                    }
                    });

                    // Add additional data like the participant
                    bool thisFound = false;
                    string dnsName = Dns.GetHostName();
                    foreach (var adActor in ad.Actors)
                    {
                        thisFound |= (adActor.NetworkAccessPointId == Environment.MachineName || adActor.NetworkAccessPointId == dnsName) &&
                            adActor.NetworkAccessPointType == SdbAudit.NetworkAccessPointType.MachineName;
                        var act = new AtnaApi.Model.AuditActorData()
                        {
                            NetworkAccessPointId = adActor.NetworkAccessPointId,
                            NetworkAccessPointType = (AtnaApi.Model.NetworkAccessPointType)Enum.Parse(typeof(AtnaApi.Model.NetworkAccessPointType), adActor.NetworkAccessPointType.ToString()),
                            NetworkAccessPointTypeSpecified = adActor.NetworkAccessPointType != 0,
                            UserIdentifier = adActor.UserIdentifier,
                            UserIsRequestor = adActor.UserIsRequestor,
                            UserName = adActor.UserName,
                            AlternativeUserId = adActor.AlternativeUserId
                        };

                        if (adActor.ActorRoleCode != null)
                            foreach (var rol in adActor.ActorRoleCode)
                                act.ActorRoleCode.Add(new CodeValue<string>(rol.Code, rol.CodeSystem)
                                {
                                    DisplayName = rol.DisplayName
                                });
                        am.Actors.Add(act);
                    }


                    foreach (var aoPtctpt in ad.AuditableObjects)
                    {
                        var atnaAo = new AtnaApi.Model.AuditableObject()
                        {
                            IDTypeCode = aoPtctpt.IDTypeCode.HasValue ?
                                aoPtctpt.IDTypeCode.Value != SdbAudit.AuditableObjectIdType.Custom ?
                                    new CodeValue<AtnaApi.Model.AuditableObjectIdType>((AtnaApi.Model.AuditableObjectIdType)Enum.Parse(typeof(AtnaApi.Model.AuditableObjectIdType), aoPtctpt?.IDTypeCode?.ToString())) :
                                    (aoPtctpt.CustomIdTypeCode != null ?
                                      new CodeValue<AtnaApi.Model.AuditableObjectIdType>()
                                      {
                                          Code = aoPtctpt.CustomIdTypeCode?.Code,
                                          CodeSystem = aoPtctpt.CustomIdTypeCode?.CodeSystem,
                                          DisplayName = aoPtctpt.CustomIdTypeCode?.DisplayName
                                      } : null) :
                                null,
                            LifecycleType = aoPtctpt.LifecycleType != SdbAudit.AuditableObjectLifecycle.NotSet && aoPtctpt.LifecycleType.HasValue ? (AtnaApi.Model.AuditableObjectLifecycle)Enum.Parse(typeof(AtnaApi.Model.AuditableObjectLifecycle), aoPtctpt.LifecycleType.ToString()) : 0,
                            LifecycleTypeSpecified = aoPtctpt.LifecycleType != SdbAudit.AuditableObjectLifecycle.NotSet && aoPtctpt.LifecycleType.HasValue,
                            ObjectId = aoPtctpt.ObjectId,
                            Role = aoPtctpt.Role.HasValue ? (AtnaApi.Model.AuditableObjectRole)Enum.Parse(typeof(AtnaApi.Model.AuditableObjectRole), aoPtctpt.Role.ToString()) : 0,
                            RoleSpecified = aoPtctpt.Role != 0,
                            Type = aoPtctpt.Type == SdbAudit.AuditableObjectType.NotSpecified ? AtnaApi.Model.AuditableObjectType.Other : (AtnaApi.Model.AuditableObjectType)Enum.Parse(typeof(AtnaApi.Model.AuditableObjectType), aoPtctpt.Type.ToString()),
                            TypeSpecified = aoPtctpt.Type != SdbAudit.AuditableObjectType.NotSpecified,
                            ObjectSpec = aoPtctpt.QueryData ?? aoPtctpt.NameData,
                            ObjectSpecChoice = aoPtctpt.QueryData == null ? ObjectDataChoiceType.ParticipantObjectName : ObjectDataChoiceType.ParticipantObjectQuery
                        };
                        // TODO: Object Data
                        foreach (var kv in aoPtctpt.ObjectData)
                            if (!String.IsNullOrEmpty(kv.Key))
                                atnaAo.ObjectDetail.Add(new ObjectDetailType()
                                {
                                    Type = kv.Key,
                                    Value = kv.Value
                                });
                        am.AuditableObjects.Add(atnaAo);
                    }

                    // Send the message
                    this.m_tracer.TraceVerbose("Dispatching audit {0} via SYSLOG", ad.Key);
                    this.m_transporter.SendMessage(am);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError(e.ToString());
                }
            }
        }

        /// <summary>
        /// Send an audit to the endpoint
        /// </summary>
        public void SendAudit(SdbAudit.AuditData ad)
        {
            if (ApplicationServiceContext.Current.IsRunning)
                ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem(SendAuditAsync, ad);
            else
                SendAuditAsync(ad);
        }

        #endregion

    }
}
