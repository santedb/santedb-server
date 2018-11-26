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
 * User: justin
 * Date: 2018-6-22
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using System;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persister that can read/write user entities
    /// </summary>
    public class UserEntityPersistenceService : EntityDerivedPersistenceService<Core.Model.Entities.UserEntity, DbUserEntity, CompositeResult<DbUserEntity, DbPerson, DbEntityVersion, DbEntity>>
    {

        // Entity persisters
        private PersonPersistenceService m_personPersister = new PersonPersistenceService();
      
        /// <summary>
        /// To model instance
        /// </summary>
        public Core.Model.Entities.UserEntity ToModelInstance(DbUserEntity userEntityInstance, DbPerson personInstance, DbEntityVersion entityVersionInstance, DbEntity entityInstance, DataContext context)
        {

            var retVal = this.m_entityPersister.ToModelInstance<UserEntity>(entityVersionInstance, entityInstance, context);
            if (retVal == null) return null;

            // Copy from person 
            retVal.DateOfBirth = personInstance?.DateOfBirth;

            // Reverse lookup
            if (!String.IsNullOrEmpty(personInstance?.DateOfBirthPrecision))
                retVal.DateOfBirthPrecision = PersonPersistenceService.PrecisionMap.Where(o => o.Value == personInstance.DateOfBirthPrecision).Select(o => o.Key).First();

            retVal.LanguageCommunication = context.Query<DbPersonLanguageCommunication>(v => v.SourceKey == entityInstance.Key && v.EffectiveVersionSequenceId <= entityVersionInstance.VersionSequenceId && (v.ObsoleteVersionSequenceId == null || v.ObsoleteVersionSequenceId >= entityVersionInstance.VersionSequenceId))
                    .Select(o => new Core.Model.Entities.PersonLanguageCommunication(o.LanguageCode, o.IsPreferred)
                    {
                        Key = o.Key
                    })
                    .ToList();

            retVal.SecurityUserKey = userEntityInstance.SecurityUserKey;
            return retVal;
        }
        
        /// <summary>
        /// Inserts the user entity
        /// </summary>
        public override Core.Model.Entities.UserEntity InsertInternal(DataContext context, Core.Model.Entities.UserEntity data)
        {
            if(data.SecurityUser != null) data.SecurityUser = data.SecurityUser?.EnsureExists(context) as SecurityUser;
            data.SecurityUserKey = data.SecurityUser?.Key ?? data.SecurityUserKey;
            var inserted = this.m_personPersister.InsertInternal(context, data);

            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update the specified user entity
        /// </summary>
        public override Core.Model.Entities.UserEntity UpdateInternal(DataContext context, Core.Model.Entities.UserEntity data)
        {
            if (data.SecurityUser != null) data.SecurityUser = data.SecurityUser?.EnsureExists(context) as SecurityUser;
            data.SecurityUserKey = data.SecurityUser?.Key ?? data.SecurityUserKey;
            this.m_personPersister.UpdateInternal(context, data);
            return base.UpdateInternal(context, data);
        }

        /// <summary>
        /// Obsolete the specified user instance
        /// </summary>
        public override Core.Model.Entities.UserEntity ObsoleteInternal(DataContext context, Core.Model.Entities.UserEntity data)
        {
            var retVal = this.m_personPersister.ObsoleteInternal(context, data);
            return data;
        }
    }
}
