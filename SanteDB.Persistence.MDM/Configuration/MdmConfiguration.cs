using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.MDM.Configuration
{
    /// <summary>
    /// Represents a configuration for MDM
    /// </summary>
    public class MdmConfiguration
    {

        /// <summary>
        /// Create a new MDM configuration
        /// </summary>
        public MdmConfiguration(IEnumerable<MdmResourceConfiguration> resourceTypes)
        {
            this.ResourceTypes = resourceTypes;
        }

        /// <summary>
        /// Gets or sets the resource types
        /// </summary>
        public IEnumerable<MdmResourceConfiguration> ResourceTypes { get; private set; }

    }
}
