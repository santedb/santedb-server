using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Attributes;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Services;
using SanteDB.Persistence.MDM.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.MDM.Services
{
    /// <summary>
    /// The MdmRecordDaemon is responsible for subscribing to MDM targets in the configuration 
    /// and linking/creating master records whenever a record of that type is created.
    /// </summary>
    [TraceSource(MdmConstants.TraceSourceName)]
    public class MdmDaemonService : IDaemonService, IDisposable
    {

        // TRace source
        private TraceSource m_traceSource = new TraceSource(MdmConstants.TraceSourceName);

        // Configuration
        private MdmConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(MdmConstants.ConfigurationSectionName) as MdmConfiguration;

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
            ApplicationContext.Current.Started += (o, e) =>
            {
                if (ApplicationContext.Current.GetService<IRecordMatchingService>() == null)
                    throw new InvalidOperationException("MDM requires a matching service to be configured");

                foreach(var itm in this.m_configuration.ResourceTypes)
                {
                    this.m_traceSource.TraceInformation("Adding MDM listener for {0}...", itm.ResourceType.Name);
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
