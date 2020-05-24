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
using RestSrvr;
using SanteDB.Authentication.OAuth2.Rest;
using SanteDB.Authentication.OAuth2.Wcf;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Rest;
using SanteDB.Core.Services;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;

namespace SanteDB.Authentication.OAuth2
{
    /// <summary>
    /// OAuth2 message handler
    /// </summary>
    [ServiceProvider("OAuth 2.0 Token Service")]
    public class OAuthMessageHandler : IDaemonService, IApiEndpointProvider
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "OAuth 2.0 Token Service";

        // Trace source
        private Tracer m_traceSource = new Tracer(OAuthConstants.TraceSourceName);

        // Service host
        private RestService m_serviceHost;

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => typeof(OAuthTokenBehavior);

        /// <summary>
        /// True if is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_serviceHost?.IsRunning == true;
            }
        }

        /// <summary>
        /// API type
        /// </summary>
        public ServiceEndpointType ApiType
        {
            get
            {
                return ServiceEndpointType.AuthenticationService;
            }
        }
        

        /// <summary>
        /// Access control
        /// </summary>
        public string[] Url
        {
            get
            {
                return this.m_serviceHost.Endpoints.Select(o => o.Description.ListenUri.ToString()).ToArray();
            }
        }

        /// <summary>
        /// Capabilities
        /// </summary>
        public ServiceEndpointCapabilities Capabilities
        {
            get
            {
                var caps = ServiceEndpointCapabilities.None;
                if (this.m_serviceHost.ServiceBehaviors.OfType<ClientAuthorizationAccessBehavior>().Any())
                    caps |= ServiceEndpointCapabilities.BasicAuth;

                return caps;
            }
        }

        /// <summary>
        /// Fired when the service is starting
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Fired when the service is stopping
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
        /// Start the specified message handler service
        /// </summary>
        public bool Start()
        {
            // Don't startup unless in SanteDB
            if (Assembly.GetEntryAssembly().GetName().Name != "SanteDB")
                return true;

            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                this.m_serviceHost = ApplicationContext.Current.GetService<IRestServiceFactory>().CreateService(typeof(OAuthTokenBehavior));
                this.m_serviceHost.AddServiceBehavior(new OAuthErrorBehavior());
                // Start the webhost
                this.m_serviceHost.Start();

                this.Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch(Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Stop the handler
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            if (this.m_serviceHost != null)
            {
                this.m_serviceHost.Stop();
                this.m_serviceHost = null;
            }

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
