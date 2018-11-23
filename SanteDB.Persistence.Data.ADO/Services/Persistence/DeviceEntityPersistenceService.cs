﻿/*
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
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.OrmLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service for a device entity
    /// </summary>
    public class DeviceEntityPersistenceService : EntityDerivedPersistenceService<Core.Model.Entities.DeviceEntity, DbDeviceEntity>
    {

        /// <summary>
        /// Convert the database representation to a model instance
        /// </summary>
        public Core.Model.Entities.DeviceEntity ToModelInstance(DbDeviceEntity deviceEntity, DbEntityVersion entityVersion, DbEntity entity, DataContext context)
        {
            var retVal = m_entityPersister.ToModelInstance<Core.Model.Entities.DeviceEntity>(entityVersion, entity, context);
            if (retVal == null) return null;

            retVal.SecurityDeviceKey = deviceEntity.SecurityDeviceKey;
            retVal.ManufacturerModelName = deviceEntity.ManufacturerModelName;
            retVal.OperatingSystemName = deviceEntity.OperatingSystemName;
            return retVal;
        }

        /// <summary>
        /// Insert the specified device entity
        /// </summary>
        public override Core.Model.Entities.DeviceEntity InsertInternal(DataContext context, Core.Model.Entities.DeviceEntity data)
        {
            if(data.SecurityDevice != null) data.SecurityDevice = data.SecurityDevice?.EnsureExists(context) as SecurityDevice;
            data.SecurityDeviceKey = data.SecurityDevice?.Key ?? data.SecurityDeviceKey;

            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Updates the specified user
        /// </summary>
        public override Core.Model.Entities.DeviceEntity UpdateInternal(DataContext context, Core.Model.Entities.DeviceEntity data)
        {
            if (data.SecurityDevice != null) data.SecurityDevice = data.SecurityDevice?.EnsureExists(context) as SecurityDevice;
            data.SecurityDeviceKey = data.SecurityDevice?.Key ?? data.SecurityDeviceKey;
            return base.UpdateInternal(context, data);
        }
    }
}
