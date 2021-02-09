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
using RestSrvr;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Services;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace SanteDB.Core
{
    /// <summary>
    /// Provides a context for components. 
    /// </summary>
    /// <remarks>Allows components to be communicate with each other via a loosely coupled
    /// broker system.</remarks>
    public class ApplicationContext : IServiceProvider, IDisposable, IApplicationServiceContext, IPolicyEnforcementService
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(ApplicationContext));

        // Lock object
        private static Object s_lockObject = new object();

        // Singleton context instance
        private static ApplicationContext s_context = null;

        // Service proider
        private DependencyServiceManager m_serviceProvider = new DependencyServiceManager();

        /// <summary>
        /// Singleton accessor
        /// </summary>
        public static ApplicationContext Current
        {
            get
            {
                if (s_context == null)
                    lock (s_lockObject)
                        if (s_context == null)
                            s_context = new ApplicationContext();
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
        /// Get the operating system type
        /// </summary>
        public OperatingSystemID OperatingSystem
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                        return OperatingSystemID.MacOS;
                    case PlatformID.Unix:
                        return OperatingSystemID.Linux;
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                    case PlatformID.Xbox:
                        return OperatingSystemID.Win32;
                    default:
                        throw new InvalidOperationException("Invalid platform");
                }
            }
        }

        /// <summary>
        /// Gets the host type
        /// </summary>
        public virtual SanteDBHostType HostType => SanteDBHostType.Server;

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "SanteDB Service Manager";

        /// <summary>
        /// Creates a new instance of the host context
        /// </summary>
        protected ApplicationContext()
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
                            Tracer.AddWriter(Activator.CreateInstance(writer.TraceWriter, writer.Filter, writer.InitializationData) as TraceWriter, writer.Filter);
#if DEBUG
                    else
                        Tracer.AddWriter(new SystemDiagnosticsTraceWriter(), System.Diagnostics.Tracing.EventLevel.LogAlways);
#endif

                    Trace.TraceInformation("STAGE1 START: Start Dependency Injection Manager");
                    this.m_serviceProvider.Start();

                    this.StartTime = DateTime.Now;

                    AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStart);

                }
                finally
                {
                    startWatch.Stop();
                }
                Trace.TraceInformation("SanteDB startup completed successfully in {0} ms...", startWatch.ElapsedMilliseconds);
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

            AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStop);

            if (this.Stopped != null)
                this.Stopped(this, null);

            this.Dispose();
        }

        /// <summary>
        /// Get all registered services
        /// </summary>
        public IEnumerable<Object> GetServices() => this.m_serviceProvider.GetServices();

        /// <summary>
        /// Get a service from this host context
        /// </summary>
        public object GetService(Type serviceType) => this.m_serviceProvider.GetService(serviceType);

        /// <summary>
        /// Get strongly typed service
        /// </summary>
        public T GetService<T>() where T : class
        {
            return this.GetService(typeof(T)) as T;
        }


        /// <summary>
        /// Add service provider type
        /// </summary>
        /// <remarks>You should really call IServiceManager.AddServiceProvider</remarks>
        public void AddServiceProvider(Type serviceType) => this.m_serviceProvider.AddServiceProvider(serviceType);


        /// <summary>
        /// Remove service provider
        /// </summary>
        public void RemoveServiceProvider(Type serviceType) => this.m_serviceProvider.RemoveServiceProvider(serviceType);

        #endregion


        #region IDisposable Members


        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this.m_serviceProvider.Dispose();
            Tracer.DisposeWriters();

        }

        /// <summary>
        /// Get all types
        /// </summary>
        public IEnumerable<Type> GetAllTypes() => this.m_serviceProvider.GetAllTypes();


        /// <summary>
        /// Demand the policy
        /// </summary>
        public void Demand(string policyId)
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, policyId).Demand();
        }

        /// <summary>
        /// Demand policy enforcement
        /// </summary>
        public void Demand(string policyId, IPrincipal principal)
        {
            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, policyId, principal).Demand();
        }

        /// <summary>
        /// Add the specified service provider
        /// </summary>
        public void AddServiceProvider(object serviceInstance) => this.m_serviceProvider.AddServiceProvider(serviceInstance);

        #endregion

    }
}
