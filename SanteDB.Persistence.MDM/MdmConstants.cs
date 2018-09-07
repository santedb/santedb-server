using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.MDM
{
    /// <summary>
    /// Represents an MDM constant
    /// </summary>
    public static class MdmConstants
    {

        /// <summary>
        /// Relationship used to represents a local/master relationship
        /// </summary>
        /// <remarks>Whenever the MDM persistence layer is used the system will link incoming records (dirty records)
        /// with a generated pristine record tagged as a master record.</remarks>
        public static readonly Guid MasterRecordRelationship = Guid.Parse("97730a52-7e30-4dcd-94cd-fd532d111578");

        /// <summary>
        /// Relationship used to represent that a local record has a high probability of being a duplicate with a master record
        /// </summary>
        public static readonly Guid DuplicateRecordRelationship = Guid.Parse("56cfb115-8207-4f89-b52e-d20dbad8f8cc");

        /// <summary>
        /// Master record classification
        /// </summary>
        public static readonly Guid MasterRecordClassification = Guid.Parse("49328452-7e30-4dcd-94cd-fd532d111578");

        /// <summary>
        /// Determiner codes
        /// </summary>
        public static readonly Guid MasterRecordDeterminer = Guid.Parse("92837281-7e30-4dcd-94cd-fd532d111578");

        /// <summary>
        /// The name of the trace source to use for the MDM logs
        /// </summary>
        public const String TraceSourceName = "SanteDB.Persistence.MDM";

        /// <summary>
        /// MDM configuration name
        /// </summary>
        public const String ConfigurationSectionName = "santedb.mdm";
    }
}
