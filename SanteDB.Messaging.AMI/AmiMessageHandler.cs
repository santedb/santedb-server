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
using RestSrvr;
using RestSrvr.Bindings;
using SanteDB.Core.Interop;
using SanteDB.Core.Rest;
using SanteDB.Core.Rest.Behavior;
using SanteDB.Core.Rest.Security;
using SanteDB.Messaging.AMI.Configuration;
using SanteDB.Messaging.AMI.Wcf;
using SanteDB.Rest.AMI;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace SanteDB.Messaging.AMI
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
    /// AMI Message handler
    /// </summary>
    [Description("AMI Message Service")]
	public class AmiMessageHandler : IDaemonService, IApiEndpointProvider
	{

        // Configuration
        private readonly AmiConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(AmiConstants.ConfigurationName) as AmiConfiguration;

        /// <summary>
        /// Resource handler tool
        /// </summary>
        internal static ResourceHandlerTool ResourceHandler { get; private set; }

        /// <summary>
        /// The internal reference to the trace source.
        /// </summary>
        private readonly TraceSource m_traceSource = new TraceSource(AmiConstants.TraceSourceName);

		/// <summary>
		/// The internal reference to the AMI configuration.
		/// </summary>
		private AmiConfiguration configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("santedb.messaging.ami") as AmiConfiguration;

		// web host
		private RestService m_webHost;

		/// <summary>
		/// Fired when the object is starting up.
		/// </summary>
		public event EventHandler Started;

		/// <summary>
		/// Fired when the object is starting.
		/// </summary>
		public event EventHandler Starting;

		/// <summary>
		/// Fired when the service has stopped.
		/// </summary>
		public event EventHandler Stopped;

		/// <summary>
		/// Fired when the service is stopping.
		/// </summary>
		public event EventHandler Stopping;

		/// <summary>
		/// Gets the API type
		/// </summary>
		public ServiceEndpointType ApiType
		{
			get
			{
				return ServiceEndpointType.AdministrationIntegrationService;
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

                this.m_webHost = RestServiceTool.CreateService(typeof(AmiServiceBehavior));
                this.m_webHost.AddServiceBehavior(new ErrorServiceBehavior());

                // Add service behaviors
                foreach (ServiceEndpoint endpoint in this.m_webHost.Endpoints)
                {
                    this.m_traceSource.TraceInformation("Starting AMI on {0}...", endpoint.Description.ListenUri);
                    endpoint.AddEndpointBehavior(new MessageCompressionEndpointBehavior());
                    endpoint.AddEndpointBehavior(new MessageDispatchFormatterBehavior());
                    endpoint.AddEndpointBehavior(new MessageLoggingEndpointBehavior());

                }

                // Start the webhost
                this.m_webHost.Start();

                AmiMessageHandler.ResourceHandler = new ResourceHandlerTool(this.configuration.ResourceHandlers);
				this.Started?.Invoke(this, EventArgs.Empty);
				return true;
			}
			catch (Exception e)
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

			if (this.m_webHost != null)
			{
				this.m_webHost.Stop();
				this.m_webHost = null;
			}

			this.Stopped?.Invoke(this, EventArgs.Empty);

			return true;
		}
	}
}