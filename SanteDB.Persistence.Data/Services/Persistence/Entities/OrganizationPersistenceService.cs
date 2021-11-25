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
    public class OrganizationPersistenceService : EntityDerivedPersistenceService<Organization>
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

        /// <summary>
        /// Do insertion of the model classes
        /// </summary>
        protected override Organization DoInsertModel(DataContext context, Organization data)
        {
            var retVal = base.DoInsertModel(context, data);
            // insert the context type
            retVal.IndustryConceptKey = context.Insert(new DbOrganization()
            {
                ParentKey = retVal.VersionKey.GetValueOrDefault(),
                IndustryConceptKey = data.IndustryConceptKey.GetValueOrDefault()
            }).IndustryConceptKey;

            return retVal;
        }

        /// <summary>
        /// Do update on the model
        /// </summary>
        protected override Organization DoUpdateModel(DataContext context, Organization data)
        {
            var retVal = base.DoUpdateModel(context, data);

            // Are we creating new versions?
            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
            {
                retVal.IndustryConceptKey = context.Insert(new DbOrganization()
                {
                    ParentKey = retVal.VersionKey.GetValueOrDefault(),
                    IndustryConceptKey = data.IndustryConceptKey.GetValueOrDefault()
                }).IndustryConceptKey;
            }
            else
            {
                retVal.IndustryConceptKey = context.Update(new DbOrganization()
                {
                    ParentKey = retVal.VersionKey.GetValueOrDefault(),
                    IndustryConceptKey = retVal.IndustryConceptKey.GetValueOrDefault()
                }).IndustryConceptKey;
            }
            return retVal;
        }

        /// <summary>
        /// Joins with <see cref="DbOrganization"/>
        /// </summary>
        public override IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<Organization, bool>> query)
        {
            return base.DoQueryInternalAs<CompositeResult<DbEntityVersion, DbOrganization>>(context, query, (o) =>
            {
                var columns = TableMapping.Get(typeof(DbOrganization)).Columns.Union(
                        TableMapping.Get(typeof(DbEntityVersion)).Columns, new ColumnMapping.ColumnComparer());
                var retVal = context.CreateSqlStatement().SelectFrom(typeof(DbEntityVersion), columns.ToArray())
                    .InnerJoin<DbEntityVersion, DbOrganization>(q => q.VersionKey, q => q.ParentKey);
                return retVal;
            });
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

            if (this.m_configuration.LoadStrategy == Configuration.LoadStrategyType.FullLoad)
            {
                retVal.IndustryConcept = this.GetRelatedPersistenceService<Concept>().Get(context, organizationData.IndustryConceptKey);
                retVal.SetLoaded(nameof(Organization.IndustryConcept));
            }
            else
            {
                retVal.IndustryConceptKey = organizationData.IndustryConceptKey;
            }

            return retVal;
        }
    }
}