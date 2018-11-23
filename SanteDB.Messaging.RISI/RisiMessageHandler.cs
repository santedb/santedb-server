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
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Interop;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel.Description;
using RestSrvr;
using SanteDB.Core.Rest.Security;
using SanteDB.Core.Rest;
using SanteDB.Core.Rest.Behavior;
using SanteDB.Messaging.RISI.Rest;

namespace SanteDB.Messaging.RISI
{
	/// <summary>
	/// Represents a message handler for reporting services.
	/// </summary>
    [Description("RISI Message Service")]
	public class RisiMessageHandler : IDaemonService, IApiEndpointProvider
	{
		/// <summary>
		/// The internal reference to the trace source.
		/// </summary>
		private readonly TraceSource traceSource = new TraceSource("SanteDB.Messaging.RISI");

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
				var caps = ServiceEndpointCapabilities.None;
                if (this.webHost.ServiceBehaviors.OfType<BasicAuthorizationAccessBehavior>().Any())
                    caps |= ServiceEndpointCapabilities.BasicAuth;
                if (this.webHost.ServiceBehaviors.OfType<TokenAuthorizationAccessBehavior>().Any())
                    caps |= ServiceEndpointCapabilities.BearerAuth;

                return caps;
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

				this.webHost = RestServiceTool.CreateService(typeof(RisiBehavior));
                this.webHost.AddServiceBehavior(new ErrorServiceBehavior());
				foreach (var endpoint in this.webHost.Endpoints)
				{
					this.traceSource.TraceInformation("Starting RISI on {0}...", endpoint.Description.ListenUri);
                    endpoint.AddEndpointBehavior(new MessageCompressionEndpointBehavior());
                    endpoint.AddEndpointBehavior(new MessageDispatchFormatterBehavior());
                    endpoint.AddEndpointBehavior(new MessageLoggingEndpointBehavior());
				}

				// Start the webhost
				this.webHost.Start();

				this.traceSource.TraceEvent(TraceEventType.Information, 0, "RISI message handler started successfully");

				this.Started?.Invoke(this, EventArgs.Empty);
			}
			catch (Exception e)
			{
				this.traceSource.TraceEvent(TraceEventType.Information, 0, "Unable to start RISI message handler");
				this.traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());

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