using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.FHIR.Util
{
    /// <summary>
    /// Get model extensions
    /// </summary>
    public static class ModelExtensions
    {

        /// <summary>
        /// Get primary code
        /// </summary>
        public static Coding GetCoding(this CodeableConcept me) => me.Coding.FirstOrDefault();
    }
}
