using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
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
    /// Persistence service which is responsible for management of non-person living subjects (like animals, food, substances, viruses, etc.)
    /// </summary>
    public class NonPersonLivingSubjectPersistenceService : EntityDerivedPersistenceService<NonPersonLivingSubject, DbNonPersonLivingSubject>
    {
        /// <inheritdoc/>
        public NonPersonLivingSubjectPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc />
        protected override NonPersonLivingSubject BeforePersisting(DataContext context, NonPersonLivingSubject data)
        {
            data.StrainKey = this.EnsureExists(context, data.Strain)?.Key ?? data.StrainKey;
            return base.BeforePersisting(context, data);
        }

        /// <summary>
        /// Convert to a sub-class
        /// </summary>
        internal override NonPersonLivingSubject DoConvertSubclassData(DataContext context, NonPersonLivingSubject modelData, DbEntityVersion dbModel, params object[] referenceObjects)
        {
            var nplsData = referenceObjects.OfType<DbNonPersonLivingSubject>().FirstOrDefault();
            if(nplsData == null )
            {
                this.m_tracer.TraceWarning("Using slow load of NonPersonLivingSubjectData to DbEntityVersion");
                nplsData = context.FirstOrDefault<DbNonPersonLivingSubject>(o => o.ParentKey == modelData.VersionKey);
            }

            switch (DataPersistenceQueryContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.Strain = this.GetRelatedPersistenceService<Concept>().Get(context, nplsData.StrainKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.Strain);
                    break;
            }
            return modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbNonPersonLivingSubject, NonPersonLivingSubject>(nplsData), false, declaredOnly: true);

        }
    }
}
