/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using System;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Localuser entity repository
    /// </summary>
    public class LocalUserEntityRepository : GenericLocalMetadataRepository<UserEntity>
    {

        /// <summary>
        /// Demand write
        /// </summary>
        public override void DemandWrite(object data)
        {
            this.ValidateWritePermission(data as UserEntity);
        }

        /// <summary>
        /// Demand alter permission
        /// </summary>
        public override void DemandAlter(object data)
        {
            this.ValidateWritePermission(data as UserEntity);
        }
        
        /// <summary>
        /// Validate that the user has write permission
        /// </summary>
        private void ValidateWritePermission(UserEntity entity)
        {
            var user = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>()?.GetUser(AuthenticationContext.Current.Principal.Identity);
            if (user?.Key != entity.SecurityUserKey)
                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AlterIdentity).Demand();
        }

        /// <summary>
        /// Insert the user entity
        /// </summary>
        public override UserEntity Insert(UserEntity entity)
        {
            return base.Insert(entity);
        }
        
        /// <summary>
        /// Obsolete the user entity
        /// </summary>
        public override UserEntity Obsolete(Guid key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Update the user entity
        /// </summary>
        public override UserEntity Save(UserEntity data)
        {
            return base.Save(data);
        }
    }
}
