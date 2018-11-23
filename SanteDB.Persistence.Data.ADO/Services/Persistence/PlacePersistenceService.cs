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
    /// Represents a persister which persists places
    /// </summary>
    public class PlacePersistenceService : EntityDerivedPersistenceService<Core.Model.Entities.Place,DbPlace>
    {
        /// <summary>
        /// Load to a model instance
        /// </summary>
        public Core.Model.Entities.Place ToModelInstance(DbPlace placeInstance, DbEntityVersion entityVersionInstance, DbEntity entityInstance, DataContext context)
        {

            var retVal = m_entityPersister.ToModelInstance<Core.Model.Entities.Place>(entityVersionInstance, entityInstance, context);
            if (retVal == null) return null;
            retVal.IsMobile = placeInstance?.IsMobile == true;
            retVal.Lat = placeInstance?.Lat;
            retVal.Lng = placeInstance?.Lng;
            return retVal;
        }

        /// <summary>
        /// Insert 
        /// </summary>
        public override Core.Model.Entities.Place InsertInternal(DataContext context, Core.Model.Entities.Place data)
        {
            var retVal = base.InsertInternal(context, data);

            if (data.Services != null)
                this.m_entityPersister.UpdateVersionedAssociatedItems<Core.Model.Entities.PlaceService,DbPlaceService>(
                   data.Services,
                    data,
                    context);

            return retVal;
        }

        /// <summary>
        /// Update the place
        /// </summary>
        public override Core.Model.Entities.Place UpdateInternal(DataContext context, Core.Model.Entities.Place data)
        {
            var retVal = base.UpdateInternal(context, data);

            if (data.Services != null)
                this.m_entityPersister.UpdateVersionedAssociatedItems<Core.Model.Entities.PlaceService,DbPlaceService>(
                   data.Services,
                    data,
                    context);

            return retVal;
        }

    }
}
