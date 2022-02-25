using SanteDB.Core.Diagnostics;
using SanteDB.Core.Jobs;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.ADO.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Jobs
{
    /// <summary>
    /// Rebuild ADO.NET Freetext index
    /// </summary>
    public class AdoRebuildFreetextIndexJob : IJob
    {

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoRebuildFreetextIndexJob));

        // Service on which SQL can be run
        private readonly AdoPersistenceConfigurationSection m_configuration;
        // State manager
        private readonly IJobStateManagerService m_stateManager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoRebuildFreetextIndexJob(IConfigurationManager configurationManager, IJobStateManagerService stateManagerService)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_stateManager = stateManagerService;
        }

        /// <summary>
        /// Get the ID of this job
        /// </summary>
        public Guid Id => Guid.Parse("4D7A0641-762F-45BC-83AF-2001887648B1");

        /// <summary>
        /// Get the name of the index
        /// </summary>
        public string Name => "Full Rebuild of Freetext Index";

        /// <inheritdoc/>
        public string Description => "Rebuilds the entire freetext index (usually the _any query parameter)";

        /// <summary>
        /// Can cancel
        /// </summary>
        public bool CanCancel => false;

        /// <summary>
        /// Gets the parameters
        /// </summary>
        public IDictionary<string, Type> Parameters => new Dictionary<String, Type>();

        /// <summary>
        /// Cancel the job
        /// </summary>
        public void Cancel()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Run the free-text indexing
        /// </summary>
        public void Run(object sender, EventArgs e, object[] parameters)
        {
            try
            {
                using(var ctx = this.m_configuration.Provider.GetWriteConnection())
                {
                    ctx.Open();
                    this.m_stateManager.SetState(this, JobStateType.Running);
                    ctx.CommandTimeout = 360000;
                    ctx.ExecuteProcedure<object>("rfrsh_fti");

                    this.m_stateManager.SetState(this, JobStateType.Completed);
                }

            }
            catch(Exception ex)
            {
                this.m_tracer.TraceError("Error refreshing ADO FreeText indexes - {0}", ex);
                this.m_stateManager.SetState(this, JobStateType.Aborted);
                this.m_stateManager.SetProgress(this, ex.Message, 0.0f);
            }
        }
    }
}
