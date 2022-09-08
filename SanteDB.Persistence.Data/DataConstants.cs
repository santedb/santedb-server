/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2022-9-7
 */
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
        /// Adhoc authority assignment key
        /// </summary>
        internal const string AdhocAuthorityAssignerKey = "ado.aa.asg.";

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