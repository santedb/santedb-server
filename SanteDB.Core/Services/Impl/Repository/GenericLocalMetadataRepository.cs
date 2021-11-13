/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SanteDB.Core.Services;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Provides generic basis for metadata editing
    /// </summary>
    public class GenericLocalMetadataRepository<TMetadata> : GenericLocalRepository<TMetadata>
        where TMetadata : IdentifiedData
    {
        /// <summary>
        /// Create a new local metadata repository
        /// </summary>
        public GenericLocalMetadataRepository(IPolicyEnforcementService policyService, ILocalizationService localizationService, IDataPersistenceService<TMetadata> dataPersistence, IPrivacyEnforcementService privacyService = null) : base(privacyService, policyService, localizationService, dataPersistence) // No need for privacy on metadata
        {
        }

        /// <summary>
        /// The query policy for metadata
        /// </summary>
        protected override string QueryPolicy => PermissionPolicyIdentifiers.ReadMetadata;

        /// <summary>
        /// The read policy for metadata
        /// </summary>
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMetadata;

        /// <summary>
        /// The write policy for metadata
        /// </summary>
        protected override string WritePolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;

        /// <summary>
        /// The delete policy for metadata
        /// </summary>
        protected override string DeletePolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;

        /// <summary>
        /// The alter policy for metadata
        /// </summary>
        protected override string AlterPolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;
    }
}