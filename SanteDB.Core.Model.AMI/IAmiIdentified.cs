using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Model.AMI
{
    /// <summary>
    /// Represents an interface to extract the URL identifier path
    /// </summary>
    public interface IAmiIdentified
    {

        /// <summary>
        /// Get the desired url resource key 
        /// </summary>
        String Key { get; set;  }

        /// <summary>
        /// Gets the tag for the resource
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// Get the modified on
        /// </summary>
        DateTimeOffset ModifiedOn { get; }
    }
}
