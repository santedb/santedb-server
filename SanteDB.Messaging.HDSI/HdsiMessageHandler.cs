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
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Interop;
using SanteDB.Rest.Common;
using SanteDB.Messaging.HDSI.Configuration;
using SanteDB.Messaging.HDSI.Wcf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using RestSrvr;
using SanteDB.Rest.HDSI;
using RestSrvr.Bindings;
using SanteDB.Core.Rest;
using SanteDB.Core.Rest.Security;
using SanteDB.Core.Rest.Behavior;

namespace SanteDB.Messaging.HDSI
{

    /// <summary>
    /// The HDSI Message Handler Daemon class
    /// </summary>
    [Description("HDSI Message Service")]
    public class HdsiMessageHandler : IMessageHandlerService, IApiEndpointProvider
    {

        /// <summary>
        /// Resource handler tool
        /// </summary>
        internal static ResourceHandlerTool ResourceHandler { get; private set; }

        // HDSI Trace host
        private TraceSource m_traceSource = new TraceSource("SanteDB.Messaging.HDSI");

        // configuration
        private HdsiConfiguration m_configuration= ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("santedb.messaging.hdsi") as HdsiConfiguration;

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
                var caps = ServiceEndpointCapabilities.Compression;
                if (this.m_webHost.ServiceBehaviors.OfType<BasicAuthorizationAccessBehavior>().Any())
                    caps |= ServiceEndpointCapabilities.BasicAuth;
                if (this.m_webHost.ServiceBehaviors.OfType<TokenAuthorizationAccessBehavior>().Any())
                    caps |= ServiceEndpointCapabilities.BearerAuth;

                return caps;
            }
        }

        /// <summary>
        /// Start the service
        /// </summary>
        public bool Start()
        {
            // Don't startup unless in SanteDB
            if (Assembly.GetEntryAssembly().GetName().Name != "SanteDB")
                return true;

            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                // Force startup
                HdsiMessageHandler.ResourceHandler = new ResourceHandlerTool(this.m_configuration.ResourceHandlers);

                this.m_webHost = RestServiceTool.CreateService(typeof(HdsiServiceBehavior));
                this.m_webHost.AddServiceBehavior(new ErrorServiceBehavior());

                // Add service behaviors
                foreach (ServiceEndpoint endpoint in this.m_webHost.Endpoints)
                {
                    this.m_traceSource.TraceInformation("Starting HDSI on {0}...", endpoint.Description.ListenUri);
                    endpoint.AddEndpointBehavior(new MessageCompressionEndpointBehavior());
                    endpoint.AddEndpointBehavior(new MessageDispatchFormatterBehavior());
                    endpoint.AddEndpointBehavior(new MessageLoggingEndpointBehavior());

                }

                // Start the webhost
                this.m_webHost.Start();

                this.Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch(Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
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
