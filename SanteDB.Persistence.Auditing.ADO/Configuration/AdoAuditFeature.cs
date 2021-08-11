using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Persistence.Auditing.ADO.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Auditing.ADO.Configuration
{
    /// <summary>
    /// ADO.NET Auditing Feature
    /// </summary>
    public class AdoAuditFeature : GenericServiceFeature<AdoAuditRepositoryService>
    {

        /// <summary>
        /// Creates a new ado audit feature
        /// </summary>
        public AdoAuditFeature()
        {
            this.Configuration = new AdoAuditConfigurationSection()
            {
                TraceSql = false
            };
        }

        /// <summary>
        /// Persistence feature
        /// </summary>
        public override string Group => FeatureGroup.Persistence;

        /// <summary>
        /// Flags for this feature
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;

        /// <summary>
        /// Gets the type of configuration
        /// </summary>
        public override Type ConfigurationType => typeof(AdoAuditConfigurationSection);
    }
}
