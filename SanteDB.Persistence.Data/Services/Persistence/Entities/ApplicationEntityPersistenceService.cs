using SanteDB.Core.Model;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Application entity persistence serivce for application entities
    /// </summary>
    public class ApplicationEntityPersistenceService : EntityDerivedPersistenceService<ApplicationEntity>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ApplicationEntityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Before persisting the data to the database
        /// </summary>
        protected override ApplicationEntity BeforePersisting(DataContext context, ApplicationEntity data)
        {
            data.SecurityApplicationKey = this.EnsureExists(context, data.SecurityApplication)?.Key ?? data.SecurityApplicationKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override ApplicationEntity DoInsertModel(DataContext context, ApplicationEntity data)
        {
            var retVal = base.DoInsertModel(context, data);
            var dbApp = this.m_modelMapper.MapModelInstance<ApplicationEntity, DbApplicationEntity>(data);
            dbApp.ParentKey = retVal.VersionKey.Value;
            dbApp = context.Insert(dbApp);
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbApplicationEntity, ApplicationEntity>(dbApp));
            return retVal;
        }

        /// <summary>
        /// Update model
        /// </summary>
        protected override ApplicationEntity DoUpdateModel(DataContext context, ApplicationEntity data)
        {
            var retVal = base.DoUpdateModel(context, data);
            var dbApp = this.m_modelMapper.MapModelInstance<ApplicationEntity, DbApplicationEntity>(data);
            dbApp.ParentKey = retVal.VersionKey.Value;
            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
            {
                dbApp = context.Insert(dbApp);
            }
            else
            {
                dbApp = context.Update(dbApp);
            }
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbApplicationEntity, ApplicationEntity>(dbApp));
            return retVal;
        }

        /// <summary>
        /// Joins with <see cref="DbOrganization"/>
        /// </summary>
        public override IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<ApplicationEntity, bool>> query)
        {
            return base.DoQueryInternalAs<CompositeResult<DbEntityVersion, DbApplicationEntity>>(context, query, (o) =>
            {
                var columns = TableMapping.Get(typeof(DbApplicationEntity)).Columns.Union(
                        TableMapping.Get(typeof(DbEntityVersion)).Columns, new ColumnMapping.ColumnComparer());
                var retVal = context.CreateSqlStatement().SelectFrom(typeof(DbEntityVersion), columns.ToArray())
                    .InnerJoin<DbEntityVersion, DbApplicationEntity>(q => q.VersionKey, q => q.ParentKey);
                return retVal;
            });
        }

        /// <summary>
        /// Convert to information model
        /// </summary>
        protected override ApplicationEntity DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            var dbApplication = referenceObjects.OfType<DbApplicationEntity>().FirstOrDefault();
            if (dbApplication == null)
            {
                this.m_tracer.TraceWarning("Using slow cross reference of application");
                dbApplication = context.FirstOrDefault<DbApplicationEntity>(o => o.ParentKey == dbModel.VersionKey);
            }

            if (this.m_configuration.LoadStrategy == Configuration.LoadStrategyType.FullLoad)
            {
                retVal.SecurityApplication = this.GetRelatedPersistenceService<SecurityApplication>().Get(context, dbApplication.SecurityApplicationKey);
                retVal.SetLoaded(nameof(ApplicationEntity.SecurityApplication));
            }
            else
            {
                retVal.SecurityApplicationKey = dbApplication.SecurityApplicationKey;
            }

            retVal.SoftwareName = dbApplication.SoftwareName;
            retVal.VendorName = dbApplication.VendorName;
            retVal.VersionName = dbApplication.VersionName;
            return retVal;
        }
    }
}