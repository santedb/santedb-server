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

        /// <inheritdoc/>
        protected override Material BeforePersisting(DataContext context, Material data)
        {
            data.FormConceptKey = this.EnsureExists(context, data.FormConcept)?.Key ?? data.FormConceptKey;
            data.QuantityConceptKey = this.EnsureExists(context, data.QuantityConcept)?.Key ?? data.QuantityConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Material DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            var materialData = referenceObjects.OfType<DbMaterial>().FirstOrDefault();
            if (materialData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbMaterial from DbEntityVersion");
                materialData = context.FirstOrDefault<DbMaterial>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.FormConcept = this.GetRelatedPersistenceService<Concept>().Get(context, materialData.FormConceptKey);
                    retVal.SetLoaded(nameof(Material.FormConcept));
                    retVal.QuantityConcept = this.GetRelatedPersistenceService<Concept>().Get(context, materialData.QuantityConceptKey);
                    retVal.SetLoaded(nameof(Material.QuantityConcept));
                    break;
            }

            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbMaterial, Material>(materialData));

            return retVal;
        }
    }
}