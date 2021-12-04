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
    public class DeviceEntityPersistenceService : EntityDerivedPersistenceService<DeviceEntity, DbDeviceEntity>
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
            data.GeoTagKey = this.EnsureExists(context, data.GeoTag)?.Key ?? data.GeoTagKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override DeviceEntity DoInsertModel(DataContext context, DeviceEntity data)
        {
            var retVal = base.DoInsertModel(context, data);

            return retVal;
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

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.SecurityDevice = this.GetRelatedPersistenceService<SecurityDevice>().Get(context, dbDevice.SecurityDeviceKey);
                    retVal.SetLoaded(nameof(DeviceEntity.SecurityDevice));
                    goto case LoadMode.SyncLoad;
                case LoadMode.SyncLoad:
                    if (dbModel.GeoTagKey.HasValue)
                    {
                        var dbGeoTag = referenceObjects.OfType<DbGeoTag>().FirstOrDefault();
                        if (dbGeoTag == null)
                        {
                            this.m_tracer.TraceWarning("Using slow geo-tag reference of device");
                            dbGeoTag = context.FirstOrDefault<DbGeoTag>(o => o.Key == dbModel.GeoTagKey);
                        }
                        retVal.GeoTag = this.GetRelatedMappingProvider<GeoTag>().ToModelInstance(context, dbGeoTag);
                        retVal.SetLoaded(nameof(DeviceEntity.GeoTag));
                    }
                    goto case LoadMode.QuickLoad;
                case LoadMode.QuickLoad:
                    retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbDeviceEntity, DeviceEntity>(dbDevice));
                    break;
            }
            return retVal;
        }
    }
}