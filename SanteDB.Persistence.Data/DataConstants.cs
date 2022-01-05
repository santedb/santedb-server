using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data
{
    /// <summary>
    /// Data constant variables
    /// </summary>
    public static class DataConstants
    {
        /// <summary>
        /// Mapper resource name
        /// </summary>
        internal const string MapResourceName = "SanteDB.Persistence.Data.Map.ModelMap.xml";

        /// <summary>
        /// Ad-hoc cache key for AA information
        /// </summary>
        internal const string AdhocAuthorityKey = "ado.aa.";

        /// <summary>
        /// Adhoc authority scope key
        /// </summary>
        internal const string AdhocAuthorityScopeKey = "ado.aa.scp.";

        /// <summary>
        /// Identity domain could not be found
        /// </summary>
        public const string IdentifierDomainNotFound = "id.aa.notFound";

        /// <summary>
        /// Identity domain applied to wrong scope
        /// </summary>
        public const string IdentifierInvalidTargetScope = "id.target";

        /// <summary>
        /// Identity domain uniqueness issue
        /// </summary>
        public const string IdentifierNotUnique = "id.unique";

        /// <summary>
        /// Principal has no authority to assign this identity domain
        /// </summary>
        public const string IdentifierNoAuthorityToAssign = "id.authority";

        /// <summary>
        /// Identifier is incorrect format
        /// </summary>
        public const string IdentifierPatternFormatFail = "id.format";

        /// <summary>
        /// Identity domain check digit provider not found
        /// </summary>
        public const string IdentifierCheckProviderNotFound = "id.check.provider";

        /// <summary>
        /// Identity domain check digit provider returned false
        /// </summary>
        public const string IdentifierCheckDigitFailed = "id.check.fail";



    }
}