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

using SanteDB.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Reflection;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using SanteDB.Core.Configuration;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Diagnostics;
using System.Security.Principal;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using SanteDB.Server.Core.Diagnostics;

namespace SanteDB.Server
{
    /// <summary>
    /// Provides a context for components.
    /// </summary>
    /// <remarks>Allows components to be communicate with each other via a loosely coupled
    /// broker system.</remarks>
    internal class ServerApplicationContext : IServiceProvider, IDisposable, IApplicationServiceContext
    {
        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(ServerApplicationContext));

        // Lock object
        private static Object s_lockObject = new object();

        // Singleton context instance
        private static ServerApplicationContext s_context = null;

        // Service proider
        private DependencyServiceManager m_serviceProvider = new DependencyServiceManager();

        /// <summary>
        /// Singleton accessor
        /// </summary>
        public static ServerApplicationContext Current
        {
            get
            {
                if (s_context == null)
                    lock (s_lockObject)
                        if (s_context == null)
                            s_context = new ServerApplicationContext();
                return s_context;
            }
            protected set
            {
                s_context = value;
            }
        }

        /// <summary>
        /// Gets the identifier for this context
        /// </summary>
        public Guid ContextId { get; protected set; }

        /// <summary>
        /// Gets the start time of the applicaton
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets whether the domain is running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the host type
        /// </summary>
        public virtual SanteDBHostType HostType => SanteDBHostType.Server;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "SanteDB Service Context";

        /// <summary>
        /// Get the version
        /// </summary>
        public string VersionString => Environment.OSVersion.VersionString;

        /// <summary>
        /// Gets the machine name
        /// </summary>
        public string MachineName => Environment.MachineName;

        /// <summary>
        /// Manufacturer name
        /// </summary>
        public string ManufacturerName => "Generic Manufacturer";

        /// <summary>
        /// Creates a new instance of the host context
        /// </summary>
        protected ServerApplicationContext()
        {
            ContextId = Guid.NewGuid();
        }

        #region IServiceProvider Members

        /// <summary>
        /// Fired when the application context starting
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Fired after application startup is complete
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Fired wehn the application context commences stop
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Fired after the appplication context is stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Start the application context
        /// </summary>
        public bool Start()
        {
            if (!this.IsRunning)
            {
                Stopwatch startWatch = new Stopwatch();

                using (AuthenticationContext.EnterSystemContext())
                {
                    try
                    {
                        startWatch.Start();

                        if (this.Starting != null)
                            this.Starting(this, null);

                        // If there is no configuration manager then add the local
                        Trace.TraceInformation("STAGE0 START: Load Configuration");

                        // Assign diagnostics
                        var config = this.GetService<IConfigurationManager>().GetSection<DiagnosticsConfigurationSection>();

                        if (config != null)
                            foreach (var writer in config.TraceWriter)
                                Tracer.AddWriter(Activator.CreateInstance(writer.TraceWriter, writer.Filter, writer.InitializationData, config.Sources.ToDictionary(o => o.SourceName, o => o.Filter)) as TraceWriter, writer.Filter);
#if DEBUG
                        else
                            Tracer.AddWriter(new SystemDiagnosticsTraceWriter(), System.Diagnostics.Tracing.EventLevel.LogAlways);
#endif

                        Trace.TraceInformation("STAGE1 START: Start Dependency Injection Manager");
                        this.m_serviceProvider.AddServiceProvider(this);
                        this.m_serviceProvider.Start();

                        Trace.TraceInformation("STAGE2 START: Notify start");
                        this.Started?.Invoke(this, EventArgs.Empty);
                        this.StartTime = DateTime.Now;

                        Trace.TraceInformation("SanteDB startup completed successfully in {0} ms...", startWatch.ElapsedMilliseconds);
                    }
                    catch (Exception e)
                    {
                        m_tracer.TraceError("Error starting up context: {0}", e);
                        this.IsRunning = false;
                        Trace.TraceWarning("Server is running in Maintenance Mode due to error {0}...", e.Message);
                    }
                    finally
                    {
                        AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStart);
                        startWatch.Stop();
                    }
                }
                this.IsRunning = true;
            }

            return true;
        }

        /// <summary>
        /// Stop the application context
        /// </summary>
        public void Stop()
        {
            if (this.Stopping != null)
                this.Stopping(this, null);

            if (this.IsRunning)
            {
                AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStop);
            }

            this.IsRunning = false;
            this.m_serviceProvider.Stop();

            if (this.Stopped != null)
                this.Stopped(this, null);

            this.Dispose();
        }

        /// <summary>
        /// Get a service from this host context
        /// </summary>
        public object GetService(Type serviceType) => this.m_serviceProvider.GetService(serviceType);

        #endregion IServiceProvider Members

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this.m_serviceProvider.Dispose();
        }

        #endregion IDisposable Members
    }
}