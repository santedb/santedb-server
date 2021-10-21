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
using SanteDB.Core;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a local entity relationship repository
    /// </summary>
    public class LocalEntityRelationshipRepository : GenericLocalRepository<EntityRelationship>
    {

        /// <summary>
        /// Entity relationship repository
        /// </summary>
        public LocalEntityRelationshipRepository(IPolicyEnforcementService policyService, ILocalizationService localizationService, IPrivacyEnforcementService privacyService = null) : base(privacyService, policyService, localizationService)
        {
        }

        /// <summary>
        /// Query policy for entities
        /// </summary>
        protected override string QueryPolicy => PermissionPolicyIdentifiers.QueryClinicalData;
        /// <summary>
        /// Read policy for entities
        /// </summary>
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadClinicalData;
        /// <summary>
        /// Write policy for entities
        /// </summary>
        protected override string WritePolicy => PermissionPolicyIdentifiers.WriteClinicalData;
        /// <summary>
        /// Delete policy for entities
        /// </summary>
        protected override string DeletePolicy => PermissionPolicyIdentifiers.DeleteClinicalData;
        /// <summary>
        /// Alter policy for entities
        /// </summary>
        protected override string AlterPolicy => PermissionPolicyIdentifiers.WriteClinicalData;

        /// <summary>
        /// Insert the entity relationship
        /// </summary>
        public override EntityRelationship Insert(EntityRelationship data)
        {
            // force set the version sequence
            if (data.EffectiveVersionSequenceId == null)
                data.EffectiveVersionSequenceId = ApplicationServiceContext.Current.GetService<IRepositoryService<Entity>>().Get(data.SourceEntityKey.Value, Guid.Empty)?.VersionSequence;

            return base.Insert(data);
        }

        /// <summary>
        /// Saves the specified data
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>TModel.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the persistence service is not found.</exception>
        public EntityRelationship Save(EntityRelationship data)
        {
            // force set the version sequence
            if (data.EffectiveVersionSequenceId == null)
                data.EffectiveVersionSequenceId = ApplicationServiceContext.Current.GetService<IRepositoryService<Entity>>().Get(data.SourceEntityKey.Value, Guid.Empty)?.VersionSequence;

            return base.Save(data);
        }

    }
}
