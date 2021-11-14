using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;

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
        public ActAssociationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// The object <paramref name="key"/> is being purged - delete all references for the object
        /// </summary>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        { }

        /// <summary>
        /// Get current version sequence
        /// </summary>
        /// <returns></returns>
        protected override long GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey)
        {
            if (context.Data.TryGetValue($"Act{sourceKey}Version", out object versionSequenceObject) && versionSequenceObject is long versionSequence)
            {
                return versionSequence;
            }
            else
            {
                versionSequence = context.Query<DbActVersion>(o => o.Key == sourceKey && !o.ObsoletionTime.HasValue).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault()?.VersionSequenceId ?? -1;
                if (versionSequence == -1)
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { id = sourceKey, type = "Act" }));
                }
                context.Data.Add($"Act{sourceKey}Version", versionSequence);
                return versionSequence;
            }
        }
    }
}