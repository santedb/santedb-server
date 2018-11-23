using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Attributes;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Tools.DataSandbox.Wcf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using RestSrvr;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Rest;

namespace SanteDB.Tools.DataSandbox
{
    /// <summary>
    /// Represents a daemon service that exports Swagger documentation
    /// </summary>
    [Description("Debugger: Data Sandbox UI")]
    [TraceSource("SanteDB.Tools.DataSandbox")]
    public class DataSandboxService : IDaemonService
    {
        // HDSI Trace host
        private TraceSource m_traceSource = new TraceSource("SanteDB.Tools.DataSandbox");

        // web host
        private RestService m_webHost;

        /// <summary>
        /// Returns true if the service is running
        /// </summary>
        public bool IsRunning { get { return this.m_webHost != null; } }

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

            ApplicationContext.Current.Started += (o, e) =>
            {

                this.m_traceSource.TraceInformation("Starting Query Builder Service...");
                
                this.m_webHost = RestServiceTool.CreateService(typeof(DataSandboxTool));
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
                this.m_traceSource.TraceInformation("Stopping Query Builder Tool...");
                this.m_webHost.Stop();
                this.m_webHost = null;
            }
            this.Stopped?.Invoke(this, EventArgs.Empty);

            return true;
        }
    }
}
