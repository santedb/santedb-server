/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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

namespace SanteDB.Core
{
    /// <summary>
    /// Provides a context for components. 
    /// </summary>
    /// <remarks>Allows components to be communicate with each other via a loosely coupled
    /// broker system.</remarks>
    public class ApplicationContext : IServiceProvider, IServiceManager, IDisposable, IApplicationServiceContext, IPolicyEnforcementService
    {

        // Lock object
        private static Object s_lockObject = new object();

        /// <summary>
        /// Singleton context instance
        /// </summary>
        protected static ApplicationContext s_context = null;

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
        /// Get the host configuration
        /// </summary>
        public ApplicationServiceContextConfigurationSection Configuration { get { return this.m_configuration; } }

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
        public bool IsRunning { get { return this.m_running; } }

        /// <summary>
        /// Get the operating system type
        /// </summary>
        public OperatingSystemID OperatingSystem
        {
            get
            {
                switch(Environment.OSVersion.Platform)
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
        /// Configuration
        /// </summary>
        private ApplicationServiceContextConfigurationSection m_configuration;

        // True with the object has been disposed
        private bool m_disposed = false;

        // Running?
        private bool m_running = false;

        /// <summary>
        /// Cached services dictionary for singleton services
        /// </summary>
        private Dictionary<Type, object> m_cachedServices = new Dictionary<Type, object>();

        // Service instances
        private List<Object> m_serviceInstances = new List<object>();

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
            if (!this.m_running)
            {
                Stopwatch startWatch = new Stopwatch();

                try
                {
                    startWatch.Start();

                    if (this.Starting != null)
                        this.Starting(this, null);

                    // If there is no configuration manager then add the local
                    Trace.TraceInformation("STAGE0 START: Load Configuration");

                    if (this.GetService<IConfigurationManager>() == null)
                        throw new InvalidOperationException("Cannot find configuration manager!");

                    this.m_configuration = this.GetService<IConfigurationManager>().GetSection<ApplicationServiceContextConfigurationSection>();

                    if (this.m_configuration == null)
                        throw new InvalidOperationException("Cannot load configuration, perhaps the services aren't installed?");

                    // Assign diagnostics
                    var config = this.GetService<IConfigurationManager>().GetSection<DiagnosticsConfigurationSection>();

                    if (config != null)
                        foreach (var writer in config.TraceWriter)
                            Tracer.AddWriter(Activator.CreateInstance(writer.TraceWriter, writer.Filter, writer.InitializationData) as TraceWriter, writer.Filter);
#if DEBUG
                    else
                        Tracer.AddWriter(new SystemDiagnosticsTraceWriter(), System.Diagnostics.Tracing.EventLevel.LogAlways);
#endif
                    // Add this
                    this.m_serviceInstances.Add(this);
                    Trace.TraceInformation("STAGE1 START: Loading services");

                    foreach (var svc in this.m_configuration.ServiceProviders)
                    {
                        if (svc.Type == null)
                            Trace.TraceWarning("Cannot find service {0}, skipping", svc.TypeXml);
                        else {
                            var spa = svc.Type.GetCustomAttribute<ServiceProviderAttribute>();
                            if (spa?.Type == ServiceInstantiationType.PerCall)
                                this.m_serviceInstances.Add(spa?.Type);
                            else if (!this.m_serviceInstances.Any(s => s.GetType() == svc.Type))
                            {
                                Trace.TraceInformation("Creating {0}...", svc.Type);
                                var instance = Activator.CreateInstance(svc.Type);
                                this.m_serviceInstances.Add(instance);
                            }
                        }
                    }

                    Trace.TraceInformation("STAGE2 START: Starting Daemons");
                    foreach (var dc in this.m_serviceInstances.OfType<IDaemonService>().ToArray())
                        if (!dc.Start())
                            throw new Exception($"Service {dc} reported unsuccessful start");

                    Trace.TraceInformation("STAGE3 START: Notify ApplicationContext has started");
                    if (this.Started != null)
                        this.Started(this, null);

                    this.StartTime = DateTime.Now;

                    AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStart);

                }
                finally
                {
                    startWatch.Stop();
                }
                Trace.TraceInformation("SanteDB startup completed successfully in {0} ms...", startWatch.ElapsedMilliseconds);
                this.m_running = true;

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

            this.m_running = false;
            
            foreach (var svc in this.m_serviceInstances.OfType<IDaemonService>().ToArray())
            {
                Trace.TraceInformation("Stopping daemon service {0}...", svc.GetType().Name);
                svc.Stop();
            }

            // Dispose services
            foreach (var svc in this.m_serviceInstances.OfType<IDisposable>().Where(o=>o != this))
                svc.Dispose();

            AuditUtil.AuditApplicationStartStop(EventTypeCodes.ApplicationStop);

            if (this.Stopped != null)
                this.Stopped(this, null);

            this.Dispose();
        }

        /// <summary>
        /// Get all registered services
        /// </summary>
        public IEnumerable<Object> GetServices()
        {
            return this.m_serviceInstances;
        }

        /// <summary>
        /// Get a service from this host context
        /// </summary>
        public object GetService(Type serviceType)
        {
            ThrowIfDisposed();

            Object candidateService = null;
            if (!this.m_cachedServices.TryGetValue(serviceType, out candidateService))
            {
                candidateService = this.m_serviceInstances.Find(o => serviceType.Equals(o) || serviceType.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo()));
                if (candidateService is Type type) // The type was registered = this is a per-call 
                    return Activator.CreateInstance(type);
                else if (candidateService != null)
                    lock (this.m_cachedServices)
                        if (!this.m_cachedServices.ContainsKey(serviceType))
                        {
                            this.m_cachedServices.Add(serviceType, candidateService);
                        }
                        else candidateService = this.m_cachedServices[serviceType];
            }
            return candidateService;
        }

        /// <summary>
        /// Get strongly typed service
        /// </summary>
        public T GetService<T>() where T : class
        {
            return this.GetService(typeof(T)) as T;
        }

        /// <summary>
        /// Throw if disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (this.m_disposed)
                throw new ObjectDisposedException(nameof(ApplicationContext));
        }


        /// <summary>
        /// Add service provider type
        /// </summary>
        public void AddServiceProvider(Type serviceType)
        {
            lock (this.m_serviceInstances)
                this.m_serviceInstances.Add(Activator.CreateInstance(serviceType));
        }


        /// <summary>
        /// Remove service provider
        /// </summary>
        public void RemoveServiceProvider(Type serviceType)
        {
            if (serviceType == typeof(ApplicationContext) || serviceType.IsAssignableFrom(typeof(ApplicationContext))) throw new InvalidOperationException("Cannot remove core application context service"); // Don't allow service to remove itself
            if (this.m_cachedServices.ContainsKey(serviceType))
                this.m_cachedServices.Remove(serviceType);
            this.m_serviceInstances.RemoveAll(o => serviceType.IsAssignableFrom(o.GetType()));

        }

        #endregion


        #region IDisposable Members


        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (this.m_disposed) return;

            this.m_disposed = true;
            foreach (var kv in this.m_serviceInstances)
                if (kv is IDisposable)
                    (kv as IDisposable).Dispose();

            Tracer.DisposeWriters();

        }

        /// <summary>
        /// Get all types
        /// </summary>
        public IEnumerable<Type> GetAllTypes()
        {
            // HACK: The wierd TRY/CATCH in select many is to prevent mono from throwning a fit
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => { try { return a.ExportedTypes; } catch { return new List<Type>(); } });
        }

      
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
        public void AddServiceProvider(object serviceInstance)
        {
            lock (this.m_serviceInstances)
                this.m_serviceInstances.Add(serviceInstance);
        }

        #endregion

    }
}
