/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-9-7
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Jobs;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Jobs
{
    /// <summary>
    /// Rebuild ADO.NET Freetext index
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AdoRebuildFreetextIndexJob : IJob
    {

        /// <summary>
        /// Job's UUID
        /// </summary>
        public const string JobUuid = "4D7A0641-762F-45BC-83AF-2001887648B1";

        // Tracer
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AdoRebuildFreetextIndexJob));

        // Service on which SQL can be run
        private readonly AdoPersistenceConfigurationSection m_configuration;
        private readonly IJobStateManagerService m_jobStateManager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoRebuildFreetextIndexJob(IConfigurationManager configurationManager, IJobStateManagerService jobStateManager)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_jobStateManager = jobStateManager;
        }

        /// <summary>
        /// Get the ID of this job
        /// </summary>
        public Guid Id => Guid.Parse(JobUuid);

        /// <summary>
        /// Get the name of the index
        /// </summary>
        public string Name => "Full Rebuild of Freetext Index";

        /// <summary>
        /// Rebuild the fulltext index
        /// </summary>
        public string Description => "Re-builds the complete full-text index for the selected ADO provider";

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
                this.m_jobStateManager.SetState(this, JobStateType.Running);
                using (var ctx = this.m_configuration.Provider.GetWriteConnection())
                {
                    ctx.Open();

                    ctx.ExecuteProcedure<object>("rfrsh_fti");

                }

                this.m_jobStateManager.SetState(this, JobStateType.Completed);
                this.m_jobStateManager.SetProgress(this, UserMessages.COMPLETE, 1.0f);

            }
            catch (Exception ex)
            {
                this.m_tracer.TraceError("Error refreshing ADO FreeText indexes - {0}", ex);
                this.m_jobStateManager.SetState(this, JobStateType.Aborted);
                this.m_jobStateManager.SetProgress(this, ex.Message, 0.0f);
                throw;
            }
        }
    }
}
