using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration.Features
{
    /// <summary>
    /// Represents a feature for the job manager
    /// </summary>
    public class JobManagerFeature : GenericServiceFeature<DefaultJobManagerService>
    {

        /// <summary>
        /// Job manager feature ctor
        /// </summary>
        public JobManagerFeature()
        {
            this.Configuration = new JobConfigurationSection()
            {
                Jobs = new List<JobItemConfiguration>()
            };
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        public override string Description => "Allows SanteDB to run scheduled or ad-hoc 'jobs' (such as compression, warehousing, backup)";

    }
}
