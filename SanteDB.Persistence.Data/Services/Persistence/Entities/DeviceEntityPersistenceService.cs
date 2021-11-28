using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.DataType;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Device entity persistence serivce for device entities
    /// </summary>
    public class DeviceEntityPersistenceService : EntityDerivedPersistenceService<DeviceEntity>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public DeviceEntityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Before persisting the data to the database
        /// </summary>
        protected override DeviceEntity BeforePersisting(DataContext context, DeviceEntity data)
        {
            data.SecurityDeviceKey = this.EnsureExists(context, data.SecurityDevice)?.Key ?? data.SecurityDeviceKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override DeviceEntity DoInsertModel(DataContext context, DeviceEntity data)
        {
            var retVal = base.DoInsertModel(context, data);
            var dbDev = this.m_modelMapper.MapModelInstance<DeviceEntity, DbDeviceEntity>(data);
            dbDev.ParentKey = retVal.VersionKey.Value;

            if (data.GeoTag != null)
            {
                dbDev.GeoTagKey = this.GetRelatedPersistenceService<GeoTag>().Insert(context, data.GeoTag).Key;
            }

            dbDev = context.Insert(dbDev);
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbDeviceEntity, DeviceEntity>(dbDev));
            return retVal;
        }

        /// <summary>
        /// Update model
        /// </summary>
        protected override DeviceEntity DoUpdateModel(DataContext context, DeviceEntity data)
        {
            var retVal = base.DoUpdateModel(context, data);
            var dbDev = this.m_modelMapper.MapModelInstance<DeviceEntity, DbDeviceEntity>(data);
            dbDev.ParentKey = retVal.VersionKey.Value;
            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.FullVersioning))
            {
                if (data.GeoTag != null)
                {
                    dbDev.GeoTagKey = this.GetRelatedPersistenceService<GeoTag>().Insert(context, data.GeoTag).Key;
                }

                dbDev = context.Insert(dbDev);
            }
            else
            {
                if (data.GeoTag != null)
                {
                    dbDev.GeoTagKey = this.GetRelatedPersistenceService<GeoTag>().Update(context, data.GeoTag).Key;
                }

                dbDev = context.Update(dbDev);
            }
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbDeviceEntity, DeviceEntity>(dbDev));
            return retVal;
        }

        /// <summary>
        /// Joins with <see cref="DbDeviceEntity"/>
        /// </summary>
        public override IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<DeviceEntity, bool>> query)
        {
            return base.DoQueryInternalAs<CompositeResult<DbEntityVersion, DbDeviceEntity>>(context, query, (o) =>
            {
                var columns = TableMapping.Get(typeof(DbDeviceEntity)).Columns.Union(
                        TableMapping.Get(typeof(DbEntityVersion)).Columns, new ColumnMapping.ColumnComparer()).Union(
                        TableMapping.Get(typeof(DbGeoTag)).Columns, new ColumnMapping.ColumnComparer());
                var retVal = context.CreateSqlStatement().SelectFrom(typeof(DbEntityVersion), columns.ToArray())
                    .InnerJoin<DbEntityVersion, DbDeviceEntity>(q => q.VersionKey, q => q.ParentKey)
                    .Join<DbDeviceEntity, DbGeoTag>("LEFT", q => q.GeoTagKey, q => q.Key);
                return retVal;
            });
        }

        /// <summary>
        /// Convert to information model
        /// </summary>
        protected override DeviceEntity DoConvertToInformationModel(DataContext context, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            var dbDevice = referenceObjects.OfType<DbDeviceEntity>().FirstOrDefault();
            if (dbDevice == null)
            {
                this.m_tracer.TraceWarning("Using slow cross reference of device");
                dbDevice = context.FirstOrDefault<DbDeviceEntity>(o => o.ParentKey == dbModel.VersionKey);
            }

            var dbGeoTag = referenceObjects.OfType<DbGeoTag>().FirstOrDefault();
            if (dbGeoTag == null)
            {
                this.m_tracer.TraceWarning("Using slow geo-tag reference of device");
                dbGeoTag = context.FirstOrDefault<DbGeoTag>(o => o.Key == dbModel.GeoTagKey);
            }

            if (this.m_configuration.LoadStrategy == Configuration.LoadStrategyType.FullLoad)
            {
                retVal.SecurityDevice = this.GetRelatedPersistenceService<SecurityDevice>().Get(context, dbDevice.SecurityDeviceKey);
                retVal.SetLoaded(nameof(DeviceEntity.SecurityDevice));
            }
            else
            {
                retVal.SecurityDeviceKey = dbDevice.SecurityDeviceKey;
            }

            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbDeviceEntity, DeviceEntity>(dbDevice));
            retVal.SetLoaded(nameof(DeviceEntity.GeoTag));

            retVal.GeoTag = this.GetRelatedMappingProvider<GeoTag>().ToModelInstance(context, dbGeoTag);
            return retVal;
        }
    }
}