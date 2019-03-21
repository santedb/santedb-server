/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using SanteDB.Persistence.MDM.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SanteDB.Persistence.MDM.Services
{
    /// <summary>
    /// The MdmRecordDaemon is responsible for subscribing to MDM targets in the configuration 
    /// and linking/creating master records whenever a record of that type is created.
    /// </summary>
    [ServiceProvider("MDM Data Repository")]
    public class MdmDaemonService : IDaemonService, IDisposable
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "MDM Daemon";

        // TRace source
        private Tracer m_traceSource = new Tracer(MdmConstants.TraceSourceName);

        // Configuration
        private MdmConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<MdmConfigurationSection>();

        // Listeners
        private List<MdmResourceListener> m_listeners = new List<MdmResourceListener>();

        // True if the service is running
        public bool IsRunning => this.m_listeners.Count > 0;

        /// <summary>
        /// Daemon is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Daemon is stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Daemon has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Daemon has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Dispose of this object
        /// </summary>
        public void Dispose()
        {
            foreach (var i in this.m_listeners)
                i.Dispose();
            this.m_listeners.Clear();
        }

        /// <summary>
        /// Start the daemon
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            // Wait until application context is started
            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                if (ApplicationServiceContext.Current.GetService<IRecordMatchingService>() == null)
                    throw new InvalidOperationException("MDM requires a matching service to be configured");

                foreach(var itm in this.m_configuration.ResourceTypes)
                {
                    this.m_traceSource.TraceInfo("Adding MDM listener for {0}...", itm.ResourceType.Name);
                    var idt = typeof(MdmResourceListener<>).MakeGenericType(itm.ResourceType);
                    this.m_listeners.Add(Activator.CreateInstance(idt, itm) as MdmResourceListener);
                }

                this.m_listeners.Add(new BundleResourceListener(this.m_listeners));
            };

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Stop the daemon
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            // Unregister
            foreach (var i in this.m_listeners)
                i.Dispose();
            this.m_listeners.Clear();

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
