/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2021-8-27
 */

using SanteDB.Core.Services;
using RestSrvr;
using SanteDB.Core;
using System;
using System.ComponentModel;
using System.Diagnostics;
using SanteDB.Core.Diagnostics;
using SanteDB.Tools.Debug.Wcf;

namespace SanteDB.Tools.Debug
{
    /// <summary>
    /// Represents a daemon service that exports Swagger documentation
    /// </summary>
    [ServiceProvider("Debugger: Data Sandbox UI")]
    public class DataSandboxService : IDaemonService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Debug Environment UI";

        // HDSI Trace host
        private readonly Tracer m_traceSource = new Tracer("SanteDB.Tools.DataSandbox");

        // web host
        private RestService m_webHost;

        /// <summary>
        /// Returns true if the service is running
        /// </summary>
        public bool IsRunning
        { get { return this.m_webHost != null; } }

        /// <summary>
        /// Fired when service has started
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Fired when service is starting
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Fired when service has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Fired when service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Start the service host
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                this.m_traceSource.TraceInfo("Starting Query Builder Service...");

                this.m_webHost = ApplicationServiceContext.Current.GetService<IRestServiceFactory>().CreateService(typeof(DataSandboxTool));
                this.m_webHost.Start();
                this.Started?.Invoke(this, EventArgs.Empty);
            };

            return true;
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            if (this.IsRunning)
            {
                this.m_traceSource.TraceInfo("Stopping Query Builder Tool...");
                this.m_webHost.Stop();
                this.m_webHost = null;
            }
            this.Stopped?.Invoke(this, EventArgs.Empty);

            return true;
        }
    }
}