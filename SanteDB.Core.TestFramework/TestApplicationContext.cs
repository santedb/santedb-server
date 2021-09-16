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
using SanteDB.Core.Configuration;
using SanteDB.Core.Data;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using SanteDB.Persistence.Data.Services;
using SanteDB.Server.Core.Diagnostics;
using SanteDB.Server.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.TestFramework
{
    /// <summary>
    /// Represents the test context
    /// </summary>
    public class TestApplicationContext : IServiceProvider, IDisposable, IApplicationServiceContext
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(TestApplicationContext));

        // Lock object
        private static Object s_lockObject = new object();

        // Singleton context instance
        private static TestApplicationContext s_context = null;

        // Service proider
        private DependencyServiceManager m_serviceProvider = new DependencyServiceManager();

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "SanteDB Test Service Context";

        /// <summary>
        /// Gets the host type
        /// </summary>
        public SanteDBHostType HostType => SanteDBHostType.Test;

        /// <summary>
        /// Gets or set sthe test assembly
        /// </summary>
        public static Assembly TestAssembly { get; set; }

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
        /// Creastes a new test context
        /// </summary>
        public TestApplicationContext()
        {
            this.ContextId = Guid.NewGuid();
            this.m_serviceProvider.AddServiceProvider(this);
            this.m_serviceProvider.AddServiceProvider(typeof(TestConfigurationService));
            this.m_serviceProvider.AddServiceProvider(typeof(DefaultThreadPoolService));
            this.m_serviceProvider.AddServiceProvider(typeof(DefaultPolicyEnforcementService));
            this.m_serviceProvider.AddServiceProvider(typeof(DefaultOperatingSystemInfoService));
        }

        /// <summary>
        /// Singleton accessor
        /// </summary>
        public static TestApplicationContext Current
        {
            get
            {
                if (s_context == null)
                    lock (s_lockObject)
                        if (s_context == null)
                            s_context = new TestApplicationContext();
                return s_context;
            }
            protected set
            {
                s_context = value;
            }
        }

        /// <summary>
        /// Initialize the test context
        /// </summary>
        /// <param name="deploymentDirectory"></param>
        public static void Initialize(String deploymentDirectory)
        {

            if (ApplicationServiceContext.Current != null) return;

            AppDomain.CurrentDomain.SetData(
               "DataDirectory",
               Path.Combine(deploymentDirectory, string.Empty));

            EntitySource.Current = new EntitySource(new PersistenceEntitySource());
            ApplicationServiceContext.Current = TestApplicationContext.Current = new TestApplicationContext();
            TestApplicationContext.Current.Start();

            // Start the daemon services
            var adoPersistenceService = ApplicationServiceContext.Current.GetService<AdoPersistenceService>();
            
        }


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
                            Tracer.AddWriter(Activator.CreateInstance(writer.TraceWriter, writer.Filter, writer.InitializationData, config.Sources.ToDictionary(o=>o.SourceName, o=>o.Filter)) as TraceWriter, writer.Filter);
#if DEBUG
                    else
                        Tracer.AddWriter(new SystemDiagnosticsTraceWriter(), System.Diagnostics.Tracing.EventLevel.LogAlways);
#endif

                    this.m_tracer.TraceInfo("STAGE1 START: Start Dependency Injection Manager");
                    this.m_serviceProvider.AddServiceProvider(this);
                    this.m_serviceProvider.Start();

                    this.m_tracer.TraceInfo("STAGE2 START: Notify start");
                    this.Started?.Invoke(this, EventArgs.Empty);
                    this.StartTime = DateTime.Now;


                }
                finally
                {
                    startWatch.Stop();
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

            this.IsRunning = false;
            this.m_serviceProvider.Stop();


            if (this.Stopped != null)
                this.Stopped(this, null);

            this.Dispose();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this.m_serviceProvider.Dispose();
            Tracer.DisposeWriters();

        }



        /// <summary>
        /// Get a service from this host context
        /// </summary>
        public object GetService(Type serviceType) => this.m_serviceProvider.GetService(serviceType);

    }
}
