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
    /// An <see cref="EntityDerivedPersistenceService{TEntity}"/> which stores and manages entities
    /// </summary>
    public class MaterialPersistenceService : EntityDerivedPersistenceService<Material, DbMaterial>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public MaterialPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Insert model class
        /// </summary>
        protected override Material DoInsertModel(DataContext context, Material data)
        {
            switch(data)
            {
                case Container ct:
                    return ct.GetRelatedPersistenceService().Insert(context, ct);
                case ManufacturedMaterial mm:
                    return mm.GetRelatedPersistenceService().Insert(context, mm);
                default:
                    return base.DoInsertModel(context, data);
            }
        }

        /// <summary>
        /// Insert model class
        /// </summary>
        protected override Material DoUpdateModel(DataContext context, Material data)
        {
            switch (data)
            {
                case Container ct:
                    return ct.GetRelatedPersistenceService().Update(context, ct);
                case ManufacturedMaterial mm:
                    return mm.GetRelatedPersistenceService().Update(context, mm);
                default:
                    return base.DoUpdateModel(context, data);
            }
        }

        /// <inheritdoc/>
        protected override Material BeforePersisting(DataContext context, Material data)
        {
            data.FormConceptKey = this.EnsureExists(context, data.FormConcept)?.Key ?? data.FormConceptKey;
            data.QuantityConceptKey = this.EnsureExists(context, data.QuantityConcept)?.Key ?? data.QuantityConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        /// </summary>
        protected override Material DoConvertToInformationModelEx(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var materialData = referenceObjects.OfType<DbMaterial>().FirstOrDefault();
            if (materialData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbMaterial from DbEntityVersion");
                materialData = context.FirstOrDefault<DbMaterial>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.FormConcept = modelData.FormConcept.GetRelatedPersistenceService().Get(context, materialData.FormConceptKey);
                    modelData.SetLoaded(o => o.FormConcept);
                    modelData.QuantityConcept = modelData.QuantityConcept.GetRelatedPersistenceService().Get(context, materialData.QuantityConceptKey);
                    modelData.SetLoaded(o => o.QuantityConcept);
                    break;
            }

            return modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbMaterial, Material>(materialData), false, declaredOnly: true);
            
        }
    }
}