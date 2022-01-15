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

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoRebuildFreetextIndexJob(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
        }

        /// <summary>
        /// Get the ID of this job
        /// </summary>
        public Guid Id => Guid.Parse("4D7A0641-762F-45BC-83AF-2001887648B1");

        /// <summary>
        /// Get the name of the index
        /// </summary>
        public string Name => "Full Rebuild of Freetext Index";

        /// <summary>
        /// Can cancel
        /// </summary>
        public bool CanCancel => false;

        /// <summary>
        /// Gets the current state
        /// </summary>
        public JobStateType CurrentState { get; private set; }

        /// <summary>
        /// Gets the parameters
        /// </summary>
        public IDictionary<string, Type> Parameters => new Dictionary<String, Type>();

        /// <summary>
        /// Last time the job was started
        /// </summary>
        public DateTime? LastStarted { get; private set; }

        /// <summary>
        /// Last time the job was finished
        /// </summary>
        public DateTime? LastFinished { get; private set; }

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
                    this.LastStarted = DateTime.Now;
                    this.CurrentState = JobStateType.Running;

                    ctx.ExecuteProcedure<object>("rfrsh_fti");

                    this.CurrentState = JobStateType.Completed;
                    this.LastFinished = DateTime.Now;
                }

            }
            catch(Exception ex)
            {
                this.m_tracer.TraceError("Error refreshing ADO FreeText indexes - {0}", ex);
                this.CurrentState = JobStateType.Aborted;
            }
        }
    }
}
