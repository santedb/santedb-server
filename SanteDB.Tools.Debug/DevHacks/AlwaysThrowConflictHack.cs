using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Tools.Debug.DevHacks
{
    /// <summary>
    /// A hack that always throws a conflict after something has been persisted such as an application or device
    /// </summary>
    public class AlwaysThrowConflictHack : IDaemonService
    {
        /// <summary>
        /// True if is running
        /// </summary>
        public bool IsRunning => false;

        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName => throw new NotImplementedException();

        public event EventHandler Starting;
        public event EventHandler Started;
        public event EventHandler Stopping;
        public event EventHandler Stopped;

        public bool Start()
        {
            throw new NotImplementedException();
        }

        public bool Stop()
        {
            throw new NotImplementedException();
        }
    }
}
