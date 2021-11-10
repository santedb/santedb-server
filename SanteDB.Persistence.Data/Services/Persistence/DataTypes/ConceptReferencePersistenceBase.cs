using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Represents the concept relationship persistence service
    /// </summary>
    public abstract class ConceptReferencePersistenceBase<TModel, TDbModel> : VersionedAssociationPersistenceService<TModel, TDbModel>
        where TModel : VersionedAssociation<Concept>, new()
        where TDbModel : DbIdentified, IDbVersionedAssociation, new()
    {
        /// <summary>
        /// Creates a DI
        /// </summary>
        public ConceptReferencePersistenceBase(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Get the most current version sequence from the database
        /// </summary>
        protected override long GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey)
        {
            if (context.Data.TryGetValue($"Concept{sourceKey}Version", out object versionSequenceObject) && versionSequenceObject is long versionSequence)
            {
                return versionSequence;
            }
            else
            {
                versionSequence = context.Query<DbConceptVersion>(o => o.Key == sourceKey && !o.ObsoletionTime.HasValue).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault()?.VersionSequenceId ?? -1;
                if (versionSequence == -1)
                {
                    throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { id = sourceKey, type = "Entity" }));
                }
                context.Data.Add($"Concept{sourceKey}Version", versionSequence);
                return versionSequence;
            }
        }
    }
}