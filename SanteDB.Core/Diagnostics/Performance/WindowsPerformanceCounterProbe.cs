using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Diagnostics.Performance
{
    /// <summary>
    /// Allows the measuring of processor time
    /// </summary>
    public class WindowsPerformanceCounterProbe : DiagnosticsProbeBase<float>, IDisposable
    {


        // Windows counter
        private PerformanceCounter m_windowsCounter = null;

        /// <summary>
        /// Processor time performance probe
        /// </summary>
        public WindowsPerformanceCounterProbe(Guid uuid, String name, String description, String category, String measure, String value) : base(name, description)
        {
            if (ApplicationContext.Current.OperatingSystem == OperatingSystemID.Win32)
            {
                this.m_windowsCounter = new PerformanceCounter(category, measure, value, true);
            }
            this.Uuid = uuid;
        }

        /// <summary>
        /// Gets the current value
        /// </summary>
        public override float Value
        {
            get
            {
                return this.m_windowsCounter?.NextValue() ?? 0;
            }
        }

        /// <summary>
        /// Gets the UUID for the counter
        /// </summary>
        public override Guid Uuid { get; }

        /// <summary>
        /// Dispose the counter
        /// </summary>
        public void Dispose()
        {
            this.m_windowsCounter.Dispose();
        }
    }
}