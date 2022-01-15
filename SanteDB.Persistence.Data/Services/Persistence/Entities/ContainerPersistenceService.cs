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
    /// Represents a persistence service that stores/reads containers
    /// </summary>
    public class ContainerPersistenceService : EntityDerivedPersistenceService<Container, DbContainer, DbMaterial>
    {

        /// <inheritdoc/>
        public ContainerPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Container BeforePersisting(DataContext context, Container data)
        {
            data.FormConceptKey = this.EnsureExists(context, data.FormConcept)?.Key ?? data.FormConceptKey;
            data.QuantityConceptKey = this.EnsureExists(context, data.QuantityConcept)?.Key ?? data.QuantityConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Container DoConvertToInformationModelEx(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var containerData = referenceObjects.OfType<DbContainer>().FirstOrDefault();
            if (containerData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbContainer from DbEntityVersion");
                containerData = context.FirstOrDefault<DbContainer>(o => o.ParentKey == dbModel.VersionKey);
            }
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

            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbMaterial, Material>(materialData), false, declaredOnly: true);
            return modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbContainer, Container>(containerData), false, declaredOnly: true);
        }
    }
}
