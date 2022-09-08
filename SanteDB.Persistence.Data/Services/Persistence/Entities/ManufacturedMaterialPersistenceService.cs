/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// A persistence service which is responsible for managing manufactured materials
    /// </summary>
    public class ManufacturedMaterialPersistenceService : EntityDerivedPersistenceService<ManufacturedMaterial, DbManufacturedMaterial, DbMaterial>
    {
        /// <inheritdoc/>
        public ManufacturedMaterialPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override ManufacturedMaterial BeforePersisting(DataContext context, ManufacturedMaterial data)
        {
            data.FormConceptKey = this.EnsureExists(context, data.FormConcept)?.Key ?? data.FormConceptKey;
            data.QuantityConceptKey = this.EnsureExists(context, data.QuantityConcept)?.Key ?? data.QuantityConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override ManufacturedMaterial DoConvertToInformationModelEx(DataContext context,  DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            // Get data material
            var manufacturedMaterialData = referenceObjects.OfType<DbManufacturedMaterial>().FirstOrDefault();
            if (manufacturedMaterialData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbMaterial from DbEntityVersion");
                manufacturedMaterialData = context.FirstOrDefault<DbManufacturedMaterial>(o => o.ParentKey == dbModel.VersionKey);
            }
            modelData = modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbManufacturedMaterial, ManufacturedMaterial>(manufacturedMaterialData), false, declaredOnly: true);
            var materialData = referenceObjects.OfType<DbMaterial>().FirstOrDefault();
            if (materialData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbMaterial from DbEntityVersion");
                materialData = context.FirstOrDefault<DbMaterial>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.FormConcept = modelData.FormConcept.GetRelatedPersistenceService().Get(context, materialData.FormConceptKey);
                    modelData.SetLoaded(o => o.FormConcept);
                    modelData.QuantityConcept = modelData.QuantityConcept.GetRelatedPersistenceService().Get(context, materialData.QuantityConceptKey);
                    modelData.SetLoaded(o => o.QuantityConcept);
                    break;
            }

            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbMaterial, Material>(materialData), false, declaredOnly: true);
            return modelData;
        }

    }
}
