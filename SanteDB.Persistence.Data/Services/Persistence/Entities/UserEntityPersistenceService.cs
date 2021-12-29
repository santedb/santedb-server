using SanteDB.Core.Model;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
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
    /// Persistence service which stores and manages <seealso cref="UserEntity"/>
    /// </summary>
    public class UserEntityPersistenceService : PersonDerivedPersistenceService<UserEntity, DbUserEntity>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public UserEntityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Called prior to persisting
        /// </summary>
        protected override UserEntity BeforePersisting(DataContext context, UserEntity data)
        {
            data.SecurityUserKey = this.EnsureExists(context, data.SecurityUser)?.Key ?? data.SecurityUserKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        internal override UserEntity DoConvertSubclassData(DataContext context, UserEntity modelData, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertSubclassData(context, modelData, dbModel, referenceObjects);

            var userData = referenceObjects.OfType<DbUserEntity>().FirstOrDefault();
            if (userData == null)
            {
                this.m_tracer.TraceWarning("Will use slow loading method for DbUserEntity from DbEntityVersion");
                userData = context.FirstOrDefault<DbUserEntity>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.SecurityUser = this.GetRelatedPersistenceService<SecurityUser>().Get(context, userData.SecurityUserKey);
                    modelData.SetLoaded(o => o.SecurityUser);
                    break;
            }
            retVal.SecurityUserKey = userData.SecurityUserKey;
            return retVal;
        }
    }

}
