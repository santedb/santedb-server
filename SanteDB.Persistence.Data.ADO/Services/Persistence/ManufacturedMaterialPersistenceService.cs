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
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;


namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Manufactured material persistence service
    /// </summary>
    public class ManufacturedMaterialPersistenceService : EntityDerivedPersistenceService<Core.Model.Entities.ManufacturedMaterial, DbManufacturedMaterial, CompositeResult<DbManufacturedMaterial, DbMaterial, DbEntityVersion, DbEntity>>
    {

        public ManufacturedMaterialPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
            this.m_materialPersister = new MaterialPersistenceService(settingsProvider);
        }

        // Material persister
        private MaterialPersistenceService m_materialPersister;

        /// <summary>
        /// Material persister
        /// </summary>
        /// <param name="dataInstance"></param>
        /// <param name="context"></param>
        /// <param name="principal"></param>
        /// <returns></returns>
        public Core.Model.Entities.ManufacturedMaterial ToModelInstance(DbManufacturedMaterial dbMmat, DbMaterial dbMat, DbEntityVersion dbEntityVersion, DbEntity dbEntity, DataContext context)
        {

            var retVal = this.m_materialPersister.ToModelInstance<Core.Model.Entities.ManufacturedMaterial>(dbMat, dbEntityVersion, dbEntity, context);
            if (retVal == null) return null;

            retVal.LotNumber = dbMmat.LotNumber;
            return retVal;

        }

        /// <summary>
        /// Insert the specified manufactured material
        /// </summary>
        public override Core.Model.Entities.ManufacturedMaterial InsertInternal(DataContext context, Core.Model.Entities.ManufacturedMaterial data)
        {
            var retVal = this.m_materialPersister.InsertInternal(context, data);
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Updates the manufactured material
        /// </summary>
        public override Core.Model.Entities.ManufacturedMaterial UpdateInternal(DataContext context, Core.Model.Entities.ManufacturedMaterial data)
        {
            var updated = this.m_materialPersister.UpdateInternal(context, data);
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Obsolete the specified manufactured material
        /// </summary>
        public override Core.Model.Entities.ManufacturedMaterial ObsoleteInternal(DataContext context, Core.Model.Entities.ManufacturedMaterial data)
        {
            var obsoleted = this.m_materialPersister.ObsoleteInternal(context, data);
            return base.InsertInternal(context, data) ;
        }
    }
}
