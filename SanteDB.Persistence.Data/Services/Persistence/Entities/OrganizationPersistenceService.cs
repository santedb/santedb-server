using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// A persistence service which is able to persist and load <see cref="Organization"/>
    /// </summary>
    public class OrganizationPersistenceService : EntityDerivedPersistenceService<Organization, DbOrganization>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public OrganizationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Organization BeforePersisting(DataContext context, Organization data)
        {
            data.IndustryConceptKey = this.EnsureExists(context, data.IndustryConcept)?.Key ?? data.IndustryConceptKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Organization DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            var organizationData = referenceObjects.OfType<DbOrganization>().FirstOrDefault();
            if (organizationData == null)
            {
                this.m_tracer.TraceWarning("Using slow join to DbOrganization from DbEntityVersion");
                organizationData = context.FirstOrDefault<DbOrganization>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.IndustryConcept = this.GetRelatedPersistenceService<Concept>().Get(context, organizationData.IndustryConceptKey);
                    retVal.SetLoaded(nameof(Organization.IndustryConcept));
                    break;
            }
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbOrganization, Organization>(organizationData));

            return retVal;
        }
    }
}