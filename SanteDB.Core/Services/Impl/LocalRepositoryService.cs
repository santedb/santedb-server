/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Daemon service which adds all the repositories for acts
    /// </summary>
    [ServiceProvider("Local (database) repository service", Dependencies = new Type[] { typeof(IDataPersistenceService) })]
    public class LocalRepositoryService : IDaemonService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local (database) repository service";

        // Trace source
        private Tracer m_tracer = new Tracer(SanteDBConstants.DataTraceSourceName);

        /// <summary>
        /// Return true if the act repository service is running
        /// </summary>
        public bool IsRunning => false;

        /// <summary>
        /// Fired when starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Fired when stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Fired when started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Fired when stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Start the service
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            // Add repository services
            List<Type> repositoryServices = new List<Type>() {
                typeof(LocalConceptRepository),
                typeof(GenericLocalMetadataRepository<IdentifierType>),
                typeof(GenericLocalConceptRepository<ReferenceTerm>),
                typeof(GenericLocalConceptRepository<CodeSystem>),
                typeof(GenericLocalConceptRepository<ConceptSet>),
                typeof(GenericLocalMetadataRepository<AssigningAuthority>),
                typeof(GenericLocalMetadataRepository<ExtensionType>),
                typeof(GenericLocalMetadataRepository<TemplateDefinition>),
                typeof(LocalBatchRepository),
                typeof(LocalMaterialRepository),
                typeof(LocalManufacturedMaterialRepository),
                typeof(LocalOrganizationRepository),
                typeof(LocalPlaceRepository),
                typeof(LocalEntityRelationshipRepository),
                typeof( GenericLocalRepositoryEx<Patient>),
                typeof(LocalExtensionTypeRepository),
                typeof(LocalSecurityApplicationRepository),
                typeof(LocalSecurityDeviceRepository),
                typeof(LocalSecurityPolicyRepository),
                typeof(LocalSecurityRoleRepositoryService),
                typeof(LocalSecurityUserRepositoryService),
                typeof(LocalUserEntityRepository),
                typeof(LocalAssigningAuthorityRepository),
                typeof(GenericLocalMetadataRepository<DeviceEntity>),
                typeof(GenericLocalMetadataRepository<ApplicationEntity>),
                typeof(LocalSecurityRepositoryService)
            };

            // Non-test environments need auditing
            if (ApplicationServiceContext.Current.HostType != SanteDBHostType.Test)
                repositoryServices.Add(typeof(LocalAuditRepository));

            foreach (var t in repositoryServices)
            {
                this.m_tracer.TraceInfo("Adding repository service for {0}...", t);
                ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(t);
            }

            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                foreach (var t in AppDomain.CurrentDomain.GetAssemblies().Where(a=>!a.IsDynamic).SelectMany(a => a.GetExportedTypes()).Where(t => typeof(IdentifiedData).IsAssignableFrom(t) && !t.IsAbstract && t.GetCustomAttribute<XmlRootAttribute>() != null && !t.ContainsGenericParameters))
                {
                    var irst = typeof(IRepositoryService<>).MakeGenericType(t);
                    var irsi = ApplicationServiceContext.Current.GetService(irst);
                    if (irsi == null)
                    {
                        if (typeof(Act).IsAssignableFrom(t))
                        {
                            this.m_tracer.TraceInfo("Adding Act repository service for {0}...", t.Name);
                            var mrst = typeof(GenericLocalActRepository<>).MakeGenericType(t);
                            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(mrst);
                        }
                        else if (typeof(Entity).IsAssignableFrom(t))
                        {
                            this.m_tracer.TraceInfo("Adding Entity repository service for {0}...", t.Name);
                            var mrst = typeof(GenericLocalClinicalDataRepository<>).MakeGenericType(t);
                            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(mrst);
                        }
                    }
                }
            };

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Stop the daemon service
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }

}