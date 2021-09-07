/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a generic resource repository factory
    /// </summary>
    [ServiceProvider("Local Data Repository Factory", Dependencies = new Type[] { typeof(IDataPersistenceService) })]
    public class LocalRepositoryFactoryService : IRepositoryServiceFactory
    {

        // Service manager
        private IServiceManager m_serviceManager;

        /// <summary>
        /// Service manager
        /// </summary>
        public LocalRepositoryFactoryService(IServiceManager serivceManager)
        {
            this.m_serviceManager = serivceManager;
        }

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Data Repository Factory";

        /// <summary>
        /// Create the specified resource service factory
        /// </summary>
        public IRepositoryService<T> CreateRepository<T>() where T : IdentifiedData
        {
            new Tracer(SanteDBConstants.DataTraceSourceName).TraceEvent(EventLevel.Warning, "Creating generic repository for {0}. Security may be compromised! Please register an appropriate repository service with the host", typeof(T).FullName);
            return this.m_serviceManager.CreateInjected<GenericLocalRepository<T>>();
        }

    }
}
