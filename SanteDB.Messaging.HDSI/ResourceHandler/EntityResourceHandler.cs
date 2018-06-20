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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Entities;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Security.Attribute;
using System.Security.Permissions;
using SanteDB.Core.Security;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
	/// <summary>
	/// Represents a resource handler for entities.
	/// </summary>
	public class EntityResourceHandler : ResourceHandlerBase<Entity>
	{

        /// <summary>
        /// Creates an entity.
        /// </summary>
        /// <param name="data">The entity to be created.</param>
        /// <param name="updateIfExists">Whether to update the entity if it exits.</param>
        /// <returns>Returns the created entity.s</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.WriteClinicalData)]
        public override Object Create(Object data, bool updateIfExists)
		{
            return base.Create(data, updateIfExists);
		}

        /// <summary>
        /// Gets an entity by id and version id.
        /// </summary>
        /// <param name="id">The id of the entity.</param>
        /// <param name="versionId">The version id of the entity.</param>
        /// <returns>Returns the entity.</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadClinicalData)]
        public override Object Get(Guid id, Guid versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Obsoletes an entity.
        /// </summary>
        /// <param name="key">The key of the entity to be obsoleted.</param>
        /// <returns>Returns the obsoleted entity.</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.DeleteClinicalData)]
        public override Object Obsolete(Guid key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Queries for an entity.
        /// </summary>
        /// <param name="queryParameters">The query parameters to use to search for the entity.</param>
        /// <returns>Returns a list of entities.</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Queries for an entity.
        /// </summary>
        /// <param name="queryParameters">The query parameters to use to search for the entity.</param>
        /// <param name="offset">The offset of the query.</param>
        /// <param name="count">The count of the query.</param>
        /// <param name="totalCount">The total count of the query.</param>
        /// <returns>Returns a list of entities.</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return base.Query(queryParameters, offset, count, out totalCount);
        }

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="data">The entity to be updated.</param>
        /// <returns>Returns the updated entity.</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.WriteClinicalData)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}
