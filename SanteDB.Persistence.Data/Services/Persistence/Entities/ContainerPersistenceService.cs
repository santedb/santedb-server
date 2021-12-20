using SanteDB.Core.Model;
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
        internal override Container DoConvertSubclassData(DataContext context, Container modelData, DbEntityVersion dbModel, params object[] referenceObjects)
        {
           
            var containerData = referenceObjects.OfType<DbContainer>().FirstOrDefault();
            if (containerData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbContainer from DbEntityVersion");
                containerData = context.FirstOrDefault<DbContainer>(o => o.ParentKey == dbModel.VersionKey);
            }

            if (this.GetRelatedPersistenceService<Material>() is EntityDerivedPersistenceService<Material> edps)
            {
                modelData = (Container)edps.DoConvertSubclassData(context, modelData, dbModel, referenceObjects);
            }

            return modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbContainer, Container>(containerData), false, declaredOnly: true);
        }
    }
}
