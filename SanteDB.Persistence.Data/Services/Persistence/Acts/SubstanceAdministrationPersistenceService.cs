using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
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
    /// Class which can persist and manage substance administrations in the database 
    /// </summary>
    public class SubstanceAdministrationPersistenceService : ActDerivedPersistenceService<SubstanceAdministration, DbSubstanceAdministration>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public SubstanceAdministrationPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override SubstanceAdministration BeforePersisting(DataContext context, SubstanceAdministration data)
        {
            data.DoseUnitKey = this.EnsureExists(context, data.DoseUnit)?.Key ?? data.DoseUnitKey;
            data.RouteKey = this.EnsureExists(context, data.Route)?.Key ?? data.RouteKey;
            data.SiteKey = this.EnsureExists(context, data.Site)?.Key ?? data.SiteKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override SubstanceAdministration DoConvertToInformationModelEx(DataContext context, DbActVersion dbModel, params object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var dbSubst = referenceObjects.OfType<DbSubstanceAdministration>().FirstOrDefault();
            if(dbSubst == null)
            {
                this.m_tracer.TraceWarning("Using slow loading for substance administration (hint: use the correct persistence service instead)");
                dbSubst = context.FirstOrDefault<DbSubstanceAdministration>(o => o.ParentKey == dbModel.VersionKey);
            }

            // Loading mode
            switch(DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    if (dbSubst != null)
                    {
                        retVal.DoseUnit = retVal.DoseUnit.GetRelatedPersistenceService().Get(context, dbSubst.DoseUnitConceptKey);
                        retVal.SetLoaded(o => o.DoseQuantity);
                        retVal.Route = retVal.Route.GetRelatedPersistenceService().Get(context, dbSubst.RouteConceptKey);
                        retVal.SetLoaded(o => o.Route);
                        retVal.Site = retVal.Site.GetRelatedPersistenceService().Get(context, dbSubst.SiteConceptKey);
                    }
                    break;
            }

            // Copy other properties to the return value
            retVal.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbSubstanceAdministration, SubstanceAdministration>(dbSubst), declaredOnly: true);
            return retVal;

        }
    }
}
