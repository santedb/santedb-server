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
using SanteDB.Core.Services;
using RestSrvr;
using SanteDB.Core.Interop;
using SanteDB.Core.Rest;
using SanteDB.Core.Rest.Behavior;
using SanteDB.Messaging.GS1.Rest;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using System.Diagnostics.Tracing;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Messaging.GS1.Configuration;

namespace SanteDB.Messaging.GS1
{
    /// <summary>
    /// Stock service message handler
    /// </summary>
    [ApiServiceProvider("GS1 BMS XML3.3 API Endpoint", typeof(IStockService), configurationType: typeof(Gs1ConfigurationSection))]
	public class StockServiceMessageHandler : IDaemonService, IApiEndpointProvider
	{
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "GS1 BMS XML3.3 API (Rest) Endpoint";

        // HDSI Trace host
        private readonly Tracer traceSource = new Tracer(Gs1Constants.TraceSourceName);

		// web host
		private RestService webHost;

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
		/// True if running
		/// </summary>
		public bool IsRunning => this.webHost?.IsRunning == true;

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => typeof(StockServiceBehavior);

        /// <summary>
        /// Gets the API type
        /// </summary>
        public ServiceEndpointType ApiType
        {
            get
            {
                return ServiceEndpointType.Gs1StockInterface;
            }
        }

        /// <summary>
        /// URL of the service
        /// </summary>
        public string[] Url
        {
            get
            {
                return this.webHost.Endpoints.OfType<ServiceEndpoint>().Select(o => o.Description.ListenUri.ToString()).ToArray();
            }
        }

        /// <summary>
        /// Capabilities
        /// </summary>
        public ServiceEndpointCapabilities Capabilities
        {
            get
            {
                return (ServiceEndpointCapabilities)ApplicationServiceContext.Current.GetService<IRestServiceFactory>().GetServiceCapabilities(this.webHost);

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

				this.webHost = ApplicationServiceContext.Current.GetService<IRestServiceFactory>().CreateService(typeof(StockServiceBehavior));
                this.webHost.AddServiceBehavior(new ErrorServiceBehavior());
				foreach (ServiceEndpoint endpoint in this.webHost.Endpoints)
				{
					this.traceSource.TraceInfo("Starting GS1 on {0}...", endpoint.Description.ListenUri);
                    endpoint.AddEndpointBehavior(new MessageLoggingEndpointBehavior());
                }
				// Start the webhost
				this.webHost.Start();

				this.Started?.Invoke(this, EventArgs.Empty);
				return true;
			}
			catch (Exception e)
			{
				this.traceSource.TraceEvent(EventLevel.Error,  e.ToString());
				return false;
			}
		}

		/// <summary>
		/// Stop the HDSI service
		/// </summary>
		public bool Stop()
		{
			this.Stopping?.Invoke(this, EventArgs.Empty);

			if (this.webHost != null)
			{
				this.webHost.Stop();
				this.webHost = null;
			}

			this.Stopped?.Invoke(this, EventArgs.Empty);

			return true;
		}
	}
}