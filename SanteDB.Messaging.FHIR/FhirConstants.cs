using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.FHIR
{
    /// <summary>
    /// FHIR constants
    /// </summary>
    public static class FhirConstants
    {

        // Address extensions
        public static String SanteDBProfile = "http://santedb.org/fhir/profile";

        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string ConfigurationSectionName = "santedb.messaging.fhir";

        /// <summary>
        /// Trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Messaging.FHIR";
    }
}
