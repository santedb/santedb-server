using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.FHIR.Attributes
{
    /// <summary>
    /// Used to identify an enumeration's FHIR valueset
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class FhirValueSetAttribute : Attribute
    {
        /// <summary>
        /// The URI to the FHIR valueset
        /// </summary>
        public String Uri { get; set; }
    }
}
