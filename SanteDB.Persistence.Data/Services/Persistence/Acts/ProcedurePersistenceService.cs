using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service which can store and retrieve patient procedures
    /// </summary>
    public class ProcedurePersistenceService : ActDerivedPersistenceService<Procedure, DbProcedure>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ProcedurePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override Procedure BeforePersisting(DataContext context, Procedure data)
        {
            data.ApproachSiteKey = this.EnsureExists(context, data.ApproachSite)?.Key ?? data.ApproachSiteKey;
            data.MethodKey = this.EnsureExists(context, data.Method)?.Key ?? data.MethodKey;
            data.TargetSiteKey = this.EnsureExists(context, data.TargetSite)?.Key ?? data.TargetSiteKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override Procedure DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var procedureData = referenceObjects.OfType<DbProcedure>().FirstOrDefault();
            if(procedureData == null)
            {
                this.m_tracer.TraceWarning("Using slow method of loading DbNarrative data from DbActVersion - Consider using the Narrative persistence service instead");
                procedureData = context.FirstOrDefault<DbProcedure>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.ApproachSite = modelData.ApproachSite.GetRelatedMappingProvider().Get(context, procedureData.ApproachSiteConceptKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.ApproachSite);
                    modelData.Method = modelData.Method.GetRelatedMappingProvider().Get(context, procedureData.MethodConceptKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.Method);
                    modelData.TargetSite = modelData.TargetSite.GetRelatedMappingProvider().Get(context, procedureData.TargetSiteConceptKey.GetValueOrDefault());
                    modelData.SetLoaded(o => o.TargetSite);
                    break;
            }

            modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbProcedure, Procedure>(procedureData), declaredOnly: true);
            return modelData;
        }
    }
}
