using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Acts;
using System;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// A generic implementation of the version association which points at an act
    /// </summary>
    public abstract class ActAssociationPersistenceService<TModel, TDbModel> : VersionedAssociationPersistenceService<TModel, TDbModel>
        where TDbModel : DbIdentified, IDbVersionedAssociation, new()
        where TModel : IdentifiedData, IVersionedAssociation, new()
    {
        /// <summary>
        /// DI injected class
        /// </summary>
        public ActAssociationPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Get current version sequence
        /// </summary>
        /// <returns></returns>
        protected override int GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey)
        {
            return context.Query<DbActVersion>(o => o.Key == sourceKey).OrderByDescending(o => o.VersionSequenceId).First().VersionSequenceId.GetValueOrDefault();
        }
    }
}