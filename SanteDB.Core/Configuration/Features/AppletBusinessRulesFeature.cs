using SanteDB.Core.Services.Daemons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration.Features
{
    /// <summary>
    /// Represents a feature for the applet business rules daemon
    /// </summary>
    public class AppletBusinessRulesFeature : GenericServiceFeature<AppletBusinessRulesDaemon>
    {

        /// <summary>
        /// Gets the grouping
        /// </summary>
        public override string Group => "Business Rules";

        /// <summary>
        /// Automatic setup of the business rules
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;
    }
}
