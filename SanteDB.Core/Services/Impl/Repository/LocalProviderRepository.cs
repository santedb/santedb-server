﻿/*
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

using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Services;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Provides operations for managing organizations.
    /// </summary>
    public class LocalProviderRepository : GenericLocalRepositoryEx<Provider>
    {
        /// <summary>
        /// Local provider repository
        /// </summary>
        public LocalProviderRepository(IPolicyEnforcementService policyService, ILocalizationService localizationService, IDataPersistenceService<Provider> persistenceService, IPrivacyEnforcementService privacyService = null) : base(policyService, localizationService, persistenceService, privacyService)
        {
        }

        protected override string QueryPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string WritePolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;
    }
}