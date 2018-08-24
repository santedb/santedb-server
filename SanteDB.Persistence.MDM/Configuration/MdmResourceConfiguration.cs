using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.MDM.Configuration
{
    /// <summary>
    /// Represents configuration for one resource
    /// </summary>
    public class MdmResourceConfiguration
    {

        /// <summary>
        /// Creates a new mdm resource configuration
        /// </summary>
        public MdmResourceConfiguration(Type resourceType, String matchConfiguration, bool merge)
        {
            this.ResourceType = resourceType;
            this.MatchConfiguration = matchConfiguration;
            this.AutoMerge = merge;
        }

        /// <summary>
        /// Gets or sets the resource type
        /// </summary>
        public Type ResourceType { get; private set; }

        /// <summary>
        /// Gets or sets the match configuration
        /// </summary>
        public String MatchConfiguration { get; private set; }

        /// <summary>
        /// Gets the auto merge attribute
        /// </summary>
        public bool AutoMerge { get; private set; }
    }
}
