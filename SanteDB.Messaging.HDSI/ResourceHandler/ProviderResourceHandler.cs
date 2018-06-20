/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * Date: 2017-9-1
 */
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Security.Permissions;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
    /// <summary>
    /// Resource handler for providers
    /// </summary>
	public class ProviderResourceHandler : ResourceHandlerBase<Provider>
	{
        /// <summary>
        /// Create the specified place
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets the specified place
        /// </summary>
        /// <returns></returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public override Object Get(Guid id, Guid versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Obsoletes the specified place
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override Object Obsolete(Guid key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Query for the specified place
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }


        /// <summary>
        /// Query for the specified place with restrictions
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return base.Query(queryParameters, offset, count, out totalCount);
        }


        /// <summary>
        /// Update the specified place
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}