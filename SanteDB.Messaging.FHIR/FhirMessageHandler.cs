using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Messaging.FHIR.Configuration;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using SanteDB.Messaging.FHIR.Util;
using SanteDB.Messaging.FHIR.Handlers;
using System.Reflection;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Messaging.FHIR.Rest;
using SanteDB.Messaging.FHIR.Rest.Serialization;
using SanteDB.Messaging.FHIR.Rest.Behavior;
using RestSrvr;
using SanteDB.Core.Rest;
using SanteDB.Core.Rest.Behavior;

namespace SanteDB.Messaging.FHIR
{
    /// <summary>
    /// Message handler for FHIR
    /// </summary>
    public class FhirMessageHandler : IMessageHandlerService
    {

        #region IMessageHandlerService Members

        private TraceSource m_traceSource = new TraceSource(FhirConstants.TraceSourceName);

        // Configuration
        private FhirServiceConfiguration m_configuration;

        // Web host
        private RestService m_webHost;

        /// <summary>
        /// Fired when the FHIR message handler is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Fired when the FHIR message handler is stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Fired when the FHIR message handler has started 
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Fired when the FHIR message handler has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Constructor, load configuration
        /// </summary>
        public FhirMessageHandler()
        {
            this.m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(FhirConstants.ConfigurationSectionName) as FhirServiceConfiguration;
        }

        /// <summary>
        /// Start the FHIR message handler
        /// </summary>
        public bool Start()
        {
            try
            {

                this.Starting?.Invoke(this, EventArgs.Empty);

                this.m_webHost = RestServiceTool.CreateService(typeof(FhirServiceBehavior));
                this.m_webHost.AddServiceBehavior(new FhirErrorEndpointBehavior());

                foreach (var endpoint in this.m_webHost.Endpoints)
                {
                    this.m_traceSource.TraceInformation("Starting FHIR on {0}...", endpoint.Description.ListenUri);

                    var corsSettings = new Core.Rest.Serialization.CorsSettings();
                    corsSettings.Resource.AddRange(this.m_configuration.CorsConfiguration.Select(o => new Core.Rest.Serialization.CorsResourceSetting(o.Key, o.Value.Domain, o.Value.Actions, o.Value.Headers)));
                    endpoint.AddEndpointBehavior(new MessageCompressionEndpointBehavior());
                    endpoint.AddEndpointBehavior(new CorsEndpointBehavior(corsSettings));
                    endpoint.AddEndpointBehavior(new MessageLoggingEndpointBehavior());

                }

                // Configuration 
                foreach (Type t in this.m_configuration.ResourceHandlers)
                {
                    ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
                    if (ci == null || t.IsAbstract)
                    {
                        this.m_traceSource.TraceEvent(TraceEventType.Warning, 0, "Type {0} has no default constructor", t.FullName);
                        continue;
                    }
                    FhirResourceHandlerUtil.RegisterResourceHandler(ci.Invoke(null) as IFhirResourceHandler);
                }

                // Start the web host
                this.m_webHost.Start();

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
        /// Stop the FHIR message handler
        /// </summary>
        /// <returns></returns>
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

        #endregion

        public bool IsRunning
        {
            get
            {
                return this.m_webHost != null;
            }
        }
    }
}
