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
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Security.Permissions;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
	/// <summary>
	/// Represents a user entity resource handler.
	/// </summary>
	public class UserEntityResourceHandler : ResourceHandlerBase<UserEntity>
	{

        /// <summary>
        /// Create the specified user entity
        /// </summary>
        public override Object Create(Object data, bool updateIfExists)
        {
            // Additional security: User should 
            var securityUser = ApplicationContext.Current.GetService<ISecurityRepositoryService>().GetUser(AuthenticationContext.Current.Principal.Identity);
            if (securityUser.Key != (data as UserEntity)?.SecurityUserKey)
                new PolicyPermission(PermissionState.Unrestricted, PermissionPolicyIdentifiers.UnrestrictedMetadata).Demand();

            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets the specified user 
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public override Object Get(object id, object versionId)
        {
            return base.Get((Guid)id, (Guid)versionId);
        }

        /// <summary>
        /// Obsolete
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override Object Obsolete(object key)
        {
            return base.Obsolete((Guid)key);
        }

        /// <summary>
        /// Query the specified data
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <summary>
        /// Query specified user
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return base.Query(queryParameters, offset, count, out totalCount);
        }

        /// <summary>
        /// Update specified data
        /// </summary>
        public override Object Update(Object data)
        {
            // Additional security: User should be admin be editing themselves
            var securityUser = ApplicationContext.Current.GetService<ISecurityRepositoryService>().GetUser(AuthenticationContext.Current.Principal.Identity);
            if (securityUser.Key != (data as UserEntity)?.SecurityUserKey)
                new PolicyPermission(PermissionState.Unrestricted, PermissionPolicyIdentifiers.UnrestrictedMetadata).Demand();

            return base.Update(data);
        }
    }
}