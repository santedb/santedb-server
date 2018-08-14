using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Data policy identifiers
    /// </summary>
    public static class DataPolicyIdentifiers
    {
        /// <summary>
        /// Represents restricted information which is only permitted for those physians directly assigned to the patient
        /// </summary>
        public const string RestrictedInformation = "1.3.6.1.4.1.33349.3.1.5.9.3";
    }
}
