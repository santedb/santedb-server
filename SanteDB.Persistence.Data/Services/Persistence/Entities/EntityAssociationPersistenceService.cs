using SanteDB.Core.i18n;
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
        protected override long GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey)
        {
            if (context.Data.TryGetValue($"Entity{sourceKey}Version", out object versionSequenceObject) && versionSequenceObject is long versionSequence)
            {
                return versionSequence;
            }
            else
            {
                versionSequence = context.Query<DbEntityVersion>(o => o.Key == sourceKey && !o.ObsoletionTime.HasValue).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault()?.VersionSequenceId ?? -1;
                if (versionSequence == -1)
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { id = sourceKey, type = "Entity" }));
                }
                context.Data.Add($"Entity{sourceKey}Version", versionSequence);
                return versionSequence;
            }
        }
    }
}