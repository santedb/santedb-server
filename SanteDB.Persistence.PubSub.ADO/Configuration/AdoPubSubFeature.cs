using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.PubSub.ADO.Configuration
{
    /// <summary>
    /// Represents an ADO persistence service
    /// </summary>
    public class AdoPubSubFeature : GenericServiceFeature<AdoPubSubManager>
    {

        /// <summary>
        /// Set the default configuration
        /// </summary>
        public AdoPubSubFeature() : base()
        {
            this.Configuration = new AdoPubSubConfigurationSection()
            {
                TraceSql = false
            };
        }

        /// <summary>
        /// Gets the type of configuration section
        /// </summary>
        public override Type ConfigurationType => typeof(AdoPubSubConfigurationSection);

        /// <summary>
        /// Automatically setup
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;

        /// <summary>
        /// Group for this setting
        /// </summary>
        public override string Group => FeatureGroup.Persistence;
    }
}
