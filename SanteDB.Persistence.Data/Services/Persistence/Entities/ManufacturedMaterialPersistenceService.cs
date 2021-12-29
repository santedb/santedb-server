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

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.FormConcept = this.GetRelatedPersistenceService<Concept>().Get(context, materialData.FormConceptKey);
                    modelData.SetLoaded(o => o.FormConcept);
                    modelData.QuantityConcept = this.GetRelatedPersistenceService<Concept>().Get(context, materialData.QuantityConceptKey);
                    modelData.SetLoaded(o => o.QuantityConcept);
                    break;
            }

            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbMaterial, Material>(materialData), false, declaredOnly: true);
            return modelData;
        }

    }
}
