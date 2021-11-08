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
        protected override int GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey)
        {
            var source = context.Query<DbConceptVersion>(o => o.Key == sourceKey).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault();
            if (source == null)
            {
                throw new KeyNotFoundException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { id = sourceKey, type = "ConceptReference" }));
            }
            return source.VersionSequenceId.GetValueOrDefault();
        }
    }
}