using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// A generic implementation of the version association which points at an act
    /// </summary>
    public abstract class EntityAssociationPersistenceService<TModel, TDbModel> : VersionedAssociationPersistenceService<TModel, TDbModel>
        where TDbModel : DbIdentified, IDbVersionedAssociation, new()
        where TModel : IdentifiedData, IVersionedAssociation, new()
    {
        /// <summary>
        /// DI injected class
        /// </summary>
        public EntityAssociationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Get current version sequence
        /// </summary>
        /// <returns></returns>
        protected override int GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey)
        {
            return context.Query<DbEntityVersion>(o => o.Key == sourceKey).OrderByDescending(o => o.VersionSequenceId).First().VersionSequenceId.GetValueOrDefault();
        }
    }
}