using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.MDM
{
    /// <summary>
    /// Represents a series of identifies related to the MDM policies for record management
    /// </summary>
    public static class MdmPermissionPolicyIdentifiers
    {

        /// <summary>
        /// The principal has unrestricted access to the MDM
        /// </summary>
        public const String UnrestrictedMdm = "1.3.6.1.4.1.33349.3.1.5.9.2.6";

        /// <summary>
        /// The principal has permission to create new master data records
        /// </summary>
        public const String WriteMdmMaster = UnrestrictedMdm + ".1";

        /// <summary>
        /// The principal has permission to read all locals from data records
        /// </summary>
        public const String ReadMdmLocals = UnrestrictedMdm + ".2";

        /// <summary>
        /// The principal has permission to merge MDM master records
        /// </summary>
        public const String MergeMdmMaster = UnrestrictedMdm + ".3";
    }
}
