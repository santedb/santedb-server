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
using SanteDB.Messaging.RISI.Rest;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SanteDB.Core;
using SanteDB.Rest.RISI;
using SanteDB.Core.Diagnostics;
using System.Diagnostics.Tracing;

namespace SanteDB.Messaging.RISI
{
    /// <summary>
    /// Represents a message handler for reporting services.
    /// </summary>
    [ServiceProvider("RISI API Daemon")]
	public class RisiMessageHandler : IDaemonService, IApiEndpointProvider
	{
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Report Integration Service API Daemon";

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => typeof(RisiBehavior);

        /// <summary>
        /// The internal reference to the trace source.
        /// </summary>
        private readonly Tracer traceSource = new Tracer("SanteDB.Messaging.RISI");

		/// <summary>
		/// The internal reference to the web host.
		/// </summary>
		private RestService webHost;

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
				return ServiceEndpointType.ReportIntegrationService;
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
        /// Gets the running state of the message handler.
        /// </summary>
        public bool IsRunning => this.webHost?.IsRunning == true;

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
		/// Starts the service. Returns true if the service started successfully.
		/// </summary>
		/// <returns>Returns true if the service started successfully.</returns>
		public bool Start()
		{

            // Don't startup unless in SanteDB
            if (Assembly.GetEntryAssembly().GetName().Name != "SanteDB")
                return true;

            try
			{
				this.Starting?.Invoke(this, EventArgs.Empty);

				this.webHost = ApplicationServiceContext.Current.GetService<IRestServiceFactory>().CreateService(typeof(RisiBehavior));
                this.webHost.AddServiceBehavior(new ErrorServiceBehavior());
				foreach (var endpoint in this.webHost.Endpoints)
				{
					this.traceSource.TraceInfo("Starting RISI on {0}...", endpoint.Description.ListenUri);
				}

				// Start the webhost
				this.webHost.Start();

				this.traceSource.TraceEvent(EventLevel.Informational, "RISI message handler started successfully");

				this.Started?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception e)
			{
				this.traceSource.TraceEvent(EventLevel.Informational, "Unable to start RISI message handler");
				this.traceSource.TraceEvent(EventLevel.Error,  e.ToString());

			}

			return true;
		}

		/// <summary>
		/// Stops the service. Returns true if the service stopped successfully.
		/// </summary>
		/// <returns>Returns true if the service stopped successfully.</returns>
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