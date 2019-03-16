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
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Configuration;
using SanteDB.Messaging.HL7.Interceptors;
using SanteDB.Messaging.HL7.TransportProtocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SanteDB.Messaging.HL7
{
    /// <summary>
    /// Message handler service
    /// </summary>
    [ServiceProvider("HL7v2 API Endpoint")]
    public class HL7MessageHandler : IDaemonService, IApiEndpointProvider
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "HL7v2 API Endpoint Provider";

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => null;

        // The local facility
        private Place m_localFacility;

        #region IMessageHandlerService Members

        // HL7 Trace source name
        private TraceSource m_traceSource = new TraceSource(Hl7Constants.TraceSourceName);
        // Configuration
        private Hl7ConfigurationSection m_configuration;

        // Threads that are listening for messages
        private List<ServiceHandler> m_listenerThreads = new List<ServiceHandler>();

        // Interceptors
        private List<InterceptorBase> m_interceptors = new List<InterceptorBase>();

        /// <summary>
        /// Start the v2 message handler
        /// </summary>
        public bool Start()
        {

            this.m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Hl7ConfigurationSection>();
            this.Starting?.Invoke(this, EventArgs.Empty);

            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                this.m_localFacility = ApplicationServiceContext.Current.GetService<IRepositoryService<Place>>()?.Get(this.m_configuration.LocalFacility);
            };

            foreach (var sd in this.m_configuration.Services)
            {
                var sh = new ServiceHandler(sd);
                Thread thdSh = new Thread(sh.Run);
                thdSh.IsBackground = true;
                thdSh.Name = $"HL7v2-{sd.Name}";
                this.m_listenerThreads.Add(sh);
                this.m_traceSource.TraceInformation("Starting HL7 Service '{0}'...", sd.Name);
                thdSh.Start();
            }

            // Interceptors
            foreach(var incptr in this.m_configuration.Interceptors
                .Select(i=> Activator.CreateInstance(i.InterceptorClass, i))
                .OfType<InterceptorBase>())
            {
                this.m_traceSource.TraceInformation("Starting Interceptor {0}...", incptr.GetType().FullName);
                this.m_interceptors.Add(incptr);
                incptr.Attach();
            }

            this.Started?.Invoke(this, EventArgs.Empty);

            return true;
        }

        /// <summary>
        /// Stop the v2 message handler
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            foreach (var thd in this.m_listenerThreads)
            {
                thd.Abort();
            }

            // Interceptors
            foreach (var incptr in this.m_interceptors)
            {
                this.m_traceSource.TraceInformation("Detaching {0}...", incptr.GetType().FullName);
                incptr.Detach();
            }

            this.m_traceSource.TraceInformation("All threads shutdown");
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        #endregion IMessageHandlerService Members

        // Host context
        private IServiceProvider m_context;

        /// <summary>
        /// Fired when the service has stopped
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Fired when the service is starting
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Fired when the service has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Fired when the service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Returns true with the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_listenerThreads?.Count > 0;
            }
        }

        /// <summary>
        /// Gets the API type
        /// </summary>
        public ServiceEndpointType ApiType => ServiceEndpointType.Hl7v2Interface;

        /// <summary>
        /// Get the URL
        /// </summary>
        public string[] Url => this.m_listenerThreads.Select(o => o.Definition.AddressXml).ToArray();

        /// <summary>
        /// Capabilities
        /// </summary>
        public ServiceEndpointCapabilities Capabilities {
            get
            {
                var retVal = ServiceEndpointCapabilities.None;
                if (this.m_listenerThreads.Any(o => o.Definition.Configuration is SllpTransport.SllpConfigurationObject))
                    retVal |= ServiceEndpointCapabilities.CertificateAuth;
                else if (this.m_configuration.Security == SecurityMethod.Msh8)
                    retVal |= ServiceEndpointCapabilities.BearerAuth;
                return retVal;
            }
        }
    }
}