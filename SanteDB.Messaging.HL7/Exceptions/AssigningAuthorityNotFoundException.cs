using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7.Exceptions
{
    /// <summary>
    /// Assigning authority was not found
    /// </summary>
    public class AssigningAuthorityNotFoundException : KeyNotFoundException
    {

        /// <summary>
        /// Gets the authority which cannot be found
        /// </summary>
        public String Authority { get; private set; }

        /// <summary>
        /// Creates a new instance of the assigning authority not found exception
        /// </summary>
        public AssigningAuthorityNotFoundException(String domainData) : base($"Assigning authority {domainData} not found")
        {
            this.Authority = domainData;
        }
    }
}
