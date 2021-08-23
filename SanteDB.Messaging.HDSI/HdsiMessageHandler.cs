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
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Server.Core.Rest;
using SanteDB.Server.Core.Rest.Behavior;
using SanteDB.Core.Services;
using SanteDB.Messaging.HDSI.Configuration;
using SanteDB.Messaging.HDSI.Wcf;
using SanteDB.Rest.Common;
using SanteDB.Rest.HDSI;
using SanteDB.Rest.HDSI.Resources;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;

namespace SanteDB.Messaging.HDSI
{

    /// <summary>
    /// The HDSI Message Handler Daemon class
    /// </summary>
    [Description("The primary iCDR Health Data Messaging Service (HDSI) allows sharing of RIM objects in XML or JSON over HTTP")]
    [ApiServiceProvider("iCDR HDSI Message Service", typeof(HdsiServiceBehavior), configurationType: typeof(HdsiConfigurationSection), required: true)]
    public class HdsiMessageHandler : IDaemonService, IApiEndpointProvider
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "iCDR Primary Clinical Messaging Interface";

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => typeof(HdsiServiceBehavior);

        /// <summary>
        /// Resource handler tool
        /// </summary>
        internal static ResourceHandlerTool ResourceHandler { get; private set; }

        // HDSI Trace host
        private Tracer m_traceSource = new Tracer(HdsiConstants.TraceSourceName);

        // configuration
        private HdsiConfigurationSection m_configuration= ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<HdsiConfigurationSection>();

        // web host
        private RestService m_webHost;

        /// <summary>
        /// True if running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_webHost?.IsRunning == true;
            }
        }

        /// <summary>
        /// Fired when the object is starting up
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Fired when the object is starting
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
        /// Gets the API type
        /// </summary>
        public ServiceEndpointType ApiType
        {
            get
            {
                return ServiceEndpointType.HealthDataService;
            }
        }

        /// <summary>
        /// URL of the service
        /// </summary>
        public string[] Url
        {
            get
            {
                return this.m_webHost.Endpoints.OfType<ServiceEndpoint>().Select(o => o.Description.ListenUri.ToString()).ToArray();
            }
        }

        /// <summary>
        /// Capabilities
        /// </summary>
        public ServiceEndpointCapabilities Capabilities
        {
            get
            {
                return (ServiceEndpointCapabilities)ApplicationServiceContext.Current.GetService<IRestServiceFactory>().GetServiceCapabilities(this.m_webHost);
            }
        }

        /// <summary>
        /// Start the service
        /// </summary>
        public bool Start()
        {
            // Don't start if we're in a test context
            if (!Assembly.GetEntryAssembly().GetName().Name.StartsWith("SanteDB"))
                return true;

            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                var serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
                // Force startup
                if(this.m_configuration?.ResourceHandlers.Count() > 0)
                    HdsiMessageHandler.ResourceHandler = new ResourceHandlerTool(this.m_configuration.ResourceHandlers.Select(o=>o.Type), typeof(IHdsiServiceContract));
                else
                    HdsiMessageHandler.ResourceHandler = new ResourceHandlerTool(

                        serviceManager.GetAllTypes()
                        .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IApiResourceHandler).IsAssignableFrom(t))
                        .ToList(), typeof(IHdsiServiceContract)
                    );
                
                this.m_webHost = ApplicationServiceContext.Current.GetService<IRestServiceFactory>().CreateService(typeof(HdsiServiceBehavior));
                this.m_webHost.AddServiceBehavior(new ErrorServiceBehavior());

                // Add service behaviors
                foreach (ServiceEndpoint endpoint in this.m_webHost.Endpoints)
                    this.m_traceSource.TraceInfo("Starting HDSI on {0}...", endpoint.Description.ListenUri);

                // Start the webhost
                ApplicationServiceContext.Current.Started += (o,e) => this.m_webHost.Start();

                this.Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch(Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Stop the HDSI service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            if(this.m_webHost != null)
            {
                this.m_webHost.Stop();
                this.m_webHost = null;
            }

            this.Stopped?.Invoke(this, EventArgs.Empty);

            return true;
        }
    }
}
