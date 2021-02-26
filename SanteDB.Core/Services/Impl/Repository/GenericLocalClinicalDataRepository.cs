﻿/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents generic local clinical data repository
    /// </summary>
    public class GenericLocalClinicalDataRepository<TModel> : GenericLocalRepositoryEx<TModel> where TModel : IdentifiedData, IHasState
    {

        /// <summary>
        /// Creates anew generic local clinic data repo
        /// </summary>
        public GenericLocalClinicalDataRepository(IPrivacyEnforcementService privacyService = null) : base(privacyService)
        {
        }

        /// <summary>
        /// The query policy for generic clinical data
        /// </summary>
        protected override string QueryPolicy => PermissionPolicyIdentifiers.QueryClinicalData;
        /// <summary>
        /// The read policy for a single piece of clinical data
        /// </summary>
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadClinicalData;
        /// <summary>
        /// The write policy for clinical data
        /// </summary>
        protected override string WritePolicy => PermissionPolicyIdentifiers.WriteClinicalData;
        /// <summary>
        /// The delete policy for clinical data
        /// </summary>
        protected override string DeletePolicy => PermissionPolicyIdentifiers.DeleteClinicalData;
        /// <summary>
        /// Alter policy for clinical data
        /// </summary>
        protected override string AlterPolicy => PermissionPolicyIdentifiers.WriteClinicalData;
    }
}