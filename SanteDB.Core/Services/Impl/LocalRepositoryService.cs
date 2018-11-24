/*
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
 * Date: 2018-6-22
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using SanteDB.Core.Model;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Interfaces;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.DataTypes;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Daemon service which adds all the repositories for acts
    /// </summary>
    public class LocalRepositoryService : IDaemonService
    {
        // Trace source
        private TraceSource m_tracer = new TraceSource(SanteDBConstants.DataTraceSourceName);

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
            Type[] repositoryServices = {
                typeof(LocalConceptRepository),
                typeof(GenericLocalMetadataRepository<IdentifierType>),
                typeof(GenericLocalConceptRepository<ReferenceTerm>),
                typeof(GenericLocalConceptRepository<CodeSystem>),
                typeof(GenericLocalConceptRepository<ConceptSet>),
                typeof(GenericLocalMetadataRepository<AssigningAuthority>),
                typeof(GenericLocalMetadataRepository<ExtensionType>),
                typeof(GenericLocalMetadataRepository<TemplateDefinition>),
                typeof(LocalMaterialRepository),
                typeof(LocalManufacturedMaterialRepository),
                typeof(LocalOrganizationRepository),
                typeof(LocalPlaceRepository),
                typeof(LocalEntityRelationshipRepository),
                typeof(LocalExtensionTypeRepository),
                typeof(LocalSecurityApplicationRepository),
                typeof(LocalSecurityDeviceRepository),
                typeof(LocalSecurityPolicyRepository),
                typeof(LocalSecurityRoleRepositoryService),
                typeof(LocalSecurityUserRepositoryService),
                typeof(LocalAssigningAuthorityRepository),
                typeof(GenericLocalMetadataRepository<DeviceEntity>),
                typeof(GenericLocalMetadataRepository<ApplicationEntity>),
                typeof(LocalSecurityRepositoryService)
            };

            foreach (var t in repositoryServices)
            {
                this.m_tracer.TraceInformation("Adding repository service for {0}...", t);
                ApplicationContext.Current.AddServiceProvider(t);
            }

            ApplicationContext.Current.Started += (o, e) =>
            {
                foreach (var t in AppDomain.CurrentDomain.GetAssemblies().Where(a=>!a.IsDynamic).SelectMany(a => a.GetExportedTypes()).Where(t => typeof(IdentifiedData).IsAssignableFrom(t) && !t.IsAbstract && t.GetCustomAttribute<XmlRootAttribute>() != null))
                {
                    var irst = typeof(IRepositoryService<>).MakeGenericType(t);
                    var irsi = ApplicationContext.Current.GetService(irst);
                    if (irsi == null)
                    {
                        if (typeof(Act).IsAssignableFrom(t))
                        {
                            this.m_tracer.TraceInformation("Adding Act repository service for {0}...", t.Name);
                            var mrst = typeof(GenericLocalActRepository<>).MakeGenericType(t);
                            ApplicationContext.Current.AddServiceProvider(mrst);
                        }
                        else if (typeof(Entity).IsAssignableFrom(t))
                        {
                            this.m_tracer.TraceInformation("Adding Entity repository service for {0}...", t.Name);
                            var mrst = typeof(GenericLocalEntityRepository<>).MakeGenericType(t);
                            ApplicationContext.Current.AddServiceProvider(mrst);
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