using System;

namespace SanteDB.Persistence.Auditing.ADO.Services
{
    /// <summary>
    /// Audit constants
    /// </summary>
    internal class AuditConstants
    {

        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string ConfigurationSectionName = "santedb.persistence.auditing.ado";

        /// <summary>
        /// Trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Auditing.ADO";

        /// <summary>
        /// The audit is new and has not been reviewed
        /// </summary>
        public readonly Guid StatusNew = Guid.Parse("21B03AD0-70FB-4BEE-A6BC-BDDFAC6BF4A4");

        /// <summary>
        /// The audit has been reviewed by a person
        /// </summary>
        public readonly Guid StatusReviewed = Guid.Parse("31B03AD0-70FB-4BEE-A6BC-BDDFAC6BF4A4");

        /// <summary>
        /// The audit has been obsoleted
        /// </summary>
        public readonly Guid StatusObsolete = Guid.Parse("41B03AD0-70FB-4BEE-A6BC-BDDFAC6BF4A4");

    }
}