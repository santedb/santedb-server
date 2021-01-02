/*
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
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persister which persists places
    /// </summary>
    public class PlacePersistenceService : EntityDerivedPersistenceService<Core.Model.Entities.Place,DbPlace>
    {

        public PlacePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Load to a model instance
        /// </summary>
        public Core.Model.Entities.Place ToModelInstance(DbPlace placeInstance, DbEntityVersion entityVersionInstance, DbEntity entityInstance, DataContext context)
        {

            var retVal = m_entityPersister.ToModelInstance<Core.Model.Entities.Place>(entityVersionInstance, entityInstance, context);
            if (retVal == null) return null;
            retVal.IsMobile = placeInstance?.IsMobile == true;

            if (placeInstance?.Lat != null || placeInstance?.Lng != null)
                retVal.GeoTag = new Core.Model.DataTypes.GeoTag(placeInstance.Lat, placeInstance.Lng, false);
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
