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
 * User: fyfej
 * Date: 2017-9-1
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Interop;
using SanteDB.Core.Wcf;
using SanteDB.Core.Wcf.Security;
using SanteDB.Messaging.Common;
using SanteDB.Messaging.HDSI.Configuration;
using SanteDB.Messaging.HDSI.ResourceHandler;
using SanteDB.Messaging.HDSI.Wcf;
using SanteDB.Messaging.HDSI.Wcf.Behavior;
using SanteDB.Messaging.HDSI.Wcf.Serialization;
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
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HDSI
{

    
    /// <summary>
    /// Http helper extensions
    /// </summary>
    public static class HttpHelperExtensions
    {

        /// <summary>
        /// Convert query types
        /// </summary>
        public static SanteDB.Core.Model.Query.NameValueCollection ToQuery(this System.Collections.Specialized.NameValueCollection nvc)
        {
            var retVal = new SanteDB.Core.Model.Query.NameValueCollection();
            foreach (var k in nvc.AllKeys)
                retVal.Add(k, new List<String>(nvc.GetValues(k)));
            return retVal;
        }
    }

    /// <summary>
    /// The HDSI Message Handler Daemon class
    /// </summary>
    [Description("HDSI Message Service")]
    public class HdsiMessageHandler : IMessageHandlerService, IApiEndpointProvider
    {

        // HDSI Trace host
        private TraceSource m_traceSource = new TraceSource("SanteDB.Messaging.HDSI");

        // configuration
        private HdsiConfiguration m_configuration= ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("santedb.messaging.hdsi") as HdsiConfiguration;

        // web host
        private WebServiceHost m_webHost;

        /// <summary>
        /// True if running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_webHost?.State == System.ServiceModel.CommunicationState.Opened;
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
                return ServiceEndpointType.ImmunizationIntegrationService;
            }
        }

        /// <summary>
        /// URL of the service
        /// </summary>
        public string[] Url
        {
            get
            {
                return this.m_webHost.Description.Endpoints.OfType<ServiceEndpoint>().Select(o => o.Address.Uri.ToString()).ToArray();
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
                if (this.m_webHost.Description.Behaviors.OfType<ServiceCredentials>().Any(o => o.UserNameAuthentication?.CustomUserNamePasswordValidator != null))
                    caps |= ServiceEndpointCapabilities.BasicAuth;
                if (this.m_webHost.Description.Behaviors.OfType<ServiceAuthorizationBehavior>().Any(o => o.ServiceAuthorizationManager is JwtTokenServiceAuthorizationManager))
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
                ResourceHandlerUtil.Current.GetType();

                this.m_webHost = new WebServiceHost(typeof(HdsiServiceBehavior));
                foreach(ServiceEndpoint endpoint in this.m_webHost.Description.Endpoints)
                {
                    this.m_traceSource.TraceInformation("Starting HDSI on {0}...", endpoint.Address);
                    (endpoint.Binding as WebHttpBinding).ContentTypeMapper = new HdsiContentTypeHandler();
                    endpoint.EndpointBehaviors.Add(new HdsiRestEndpointBehavior());
                    endpoint.EndpointBehaviors.Add(new HdsiErrorEndpointBehavior());
                }
                
                // Start the webhost
                this.m_webHost.Open();

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
                this.m_webHost.Close();
                this.m_webHost = null;
            }

            this.Stopped?.Invoke(this, EventArgs.Empty);

            return true;
        }
    }
}
