﻿/*
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
using MARC.HI.EHRS.SVC.Core.Services;
using RestSrvr;
using SanteDB.Authentication.OAuth2.Rest;
using SanteDB.Authentication.OAuth2.Wcf;
using SanteDB.Core.Interop;
using SanteDB.Core.Rest;
using SanteDB.Core.Rest.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2
{
    /// <summary>
    /// OAuth2 message handler
    /// </summary>
    public class OAuthMessageHandler : IMessageHandlerService, IApiEndpointProvider
    {

        // Trace source
        private TraceSource m_traceSource = new TraceSource(OAuthConstants.TraceSourceName);

        // Service host
        private RestService m_serviceHost;

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

                this.m_serviceHost = RestServiceTool.CreateService(typeof(OAuthTokenBehavior));
                this.m_serviceHost.AddServiceBehavior(new OAuthErrorBehavior());
                // Start the webhost
                this.m_serviceHost.Start();

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
