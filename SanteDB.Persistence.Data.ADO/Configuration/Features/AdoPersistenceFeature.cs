using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.OrmLite.Configuration;
using SanteDB.OrmLite.Migration;
using SanteDB.Persistence.Data.ADO.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Configuration.Features
{
    /// <summary>
    /// Represents an ADO persistence service
    /// </summary>
    public class AdoPersistenceFeature : GenericServiceFeature<AdoPersistenceService>
    {

        /// <summary>
        /// Set the default configuration
        /// </summary>
        public AdoPersistenceFeature() : base()
        {
            this.Configuration = new AdoPersistenceConfigurationSection()
            {
                AutoInsertChildren = true,
                AutoUpdateExisting = true,
                PrepareStatements = true
            };
        }

        /// <summary>
        /// Flags are for auto setup
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;

        /// <summary>
        /// Create the installation tasks
        /// </summary>
        public override IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            // Add installation tasks
            List<IConfigurationTask> retVal = new List<IConfigurationTask>(base.CreateInstallTasks());
            var conf = this.Configuration as OrmConfigurationBase;

            foreach (var feature in SqlFeatureUtil.GetFeatures(conf.Provider.Invariant).OfType<SqlFeature>().Where(o => o.Scope == "SanteDB.Persistence.Data.ADO").OrderBy(o=>o.Id))
                retVal.Add(new SqlMigrationTask(this, feature));
            return retVal;
        }
    }
}
