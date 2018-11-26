using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Jobs
{
    /// <summary>
    /// Job manager service
    /// </summary>
    public interface IJobManagerService : IDaemonService
    {

        /// <summary>
        /// Add a job
        /// </summary>
        void AddJob(IJob jobType, TimeSpan elapseTime);

        /// <summary>
        /// Returns true if the job is registered
        /// </summary>
        bool IsJobRegistered(Type jobType);

        /// <summary>
        /// Gets the status of all jobs
        /// </summary>
        IEnumerable<IJob> Jobs { get; }
    }
}
