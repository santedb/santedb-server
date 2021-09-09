/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Daemon service which adds all the repositories for acts
    /// </summary>
    [ServiceProvider("Local (database) repository service", Dependencies = new Type[] { typeof(IDataPersistenceService) })]
    public class LocalRepositoryService : IDaemonService, IServiceFactory
    {

        // Repository services
        private readonly Type[] r_repositoryServices = new Type[] {
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
                typeof(LocalPatientRepository),
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
                typeof(LocalSecurityRepositoryService),
                typeof(LocalTemplateDefinitionRepositoryService),
                typeof(LocalAuditRepository)
            };

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local (database) repository service";

        // Trace source
        private Tracer m_tracer = new Tracer(SanteDBConstants.DataTraceSourceName);
        private IServiceManager m_serviceManager;

        /// <summary>
        /// Return true if the act repository service is running
        /// </summary>
        public bool IsRunning => false;

        /// <summary>
        /// Create new local repository service
        /// </summary>
        public LocalRepositoryService(IServiceManager serviceManager)
        {
            this.m_serviceManager = serviceManager;
        }

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


            foreach (var t in r_repositoryServices)
            {
                this.m_tracer.TraceInfo("Adding repository service for {0}...", t);
                this.m_serviceManager.AddServiceProvider(t);
            }

            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                var types = this.m_serviceManager.GetAllTypes();

                foreach (var t in types.Where(t => typeof(IdentifiedData).IsAssignableFrom(t) && !t.IsAbstract && t.GetCustomAttribute<XmlRootAttribute>() != null && !t.ContainsGenericParameters))
                {
                    var irst = typeof(IRepositoryService<>).MakeGenericType(t);
                    var irsi = ApplicationServiceContext.Current.GetService(irst);
                    if (irsi == null)
                    {
                        if (typeof(Act).IsAssignableFrom(t))
                        {
                            this.m_tracer.TraceInfo("Adding Act repository service for {0}...", t.Name);
                            var mrst = typeof(GenericLocalActRepository<>).MakeGenericType(t);
                            this.m_serviceManager.AddServiceProvider(mrst);
                        }
                        else if (typeof(Entity).IsAssignableFrom(t))
                        {
                            this.m_tracer.TraceInfo("Adding Entity repository service for {0}...", t.Name);
                            var mrst = typeof(GenericLocalClinicalDataRepository<>).MakeGenericType(t);
                            this.m_serviceManager.AddServiceProvider(mrst);
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

        /// <summary>
        /// Try to create <typeparamref name="TService"/>
        /// </summary>
        public bool TryCreateService<TService>(out TService serviceInstance)
        {
            if(this.TryCreateService(typeof(TService), out object service))
            {
                serviceInstance = (TService)service;
                return true;
            }
            serviceInstance = default(TService);
            return false;
        }

        /// <summary>
        /// Attempt to create the specified service
        /// </summary>
        public bool TryCreateService(Type serviceType, out object serviceInstance)
        {

            // Is this service type in the services?
            var st = r_repositoryServices.FirstOrDefault(s => s == serviceType || serviceType.IsAssignableFrom(s));
            if(st == null && typeof(IRepositoryService).IsAssignableFrom(serviceType) && serviceType.IsGenericType)
            {
                var wrappedType = serviceType.GenericTypeArguments[0];
                var irst = typeof(IRepositoryService<>).MakeGenericType(wrappedType);
                var irsi = ApplicationServiceContext.Current.GetService(irst);
                if (irsi == null)
                {
                    if (typeof(Act).IsAssignableFrom(wrappedType))
                    {
                        this.m_tracer.TraceInfo("Adding Act repository service for {0}...", wrappedType.Name);
                        st = typeof(GenericLocalActRepository<>).MakeGenericType(wrappedType);
                    }
                    else if (typeof(Entity).IsAssignableFrom(wrappedType))
                    {
                        this.m_tracer.TraceInfo("Adding Entity repository service for {0}...", wrappedType);
                        st = typeof(GenericLocalClinicalDataRepository<>).MakeGenericType(wrappedType);
                    }
                }
            }
            else if(st == null)
            {
                serviceInstance = null;
                return false;
            }

            serviceInstance = this.m_serviceManager.CreateInjected(st);
            return true;

        }
    }

}