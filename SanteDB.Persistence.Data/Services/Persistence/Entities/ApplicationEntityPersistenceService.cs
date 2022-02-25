using SanteDB.Core.Model;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Entities
{
    /// <summary>
    /// Application entity persistence serivce for application entities
    /// </summary>
    public class ApplicationEntityPersistenceService : EntityDerivedPersistenceService<ApplicationEntity, DbApplicationEntity>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ApplicationEntityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Before persisting the data to the database
        /// </summary>
        protected override ApplicationEntity BeforePersisting(DataContext context, ApplicationEntity data)
        {
            data.SecurityApplicationKey = this.EnsureExists(context, data.SecurityApplication)?.Key ?? data.SecurityApplicationKey;
            return base.BeforePersisting(context, data);
        }

        /// <inheritdoc/>
        protected override ApplicationEntity DoConvertToInformationModelEx(DataContext context,  DbEntityVersion dbModel, params object[] referenceObjects)
        {

            var modelData = base.DoConvertToInformationModelEx(context, dbModel, referenceObjects);
            var dbApplication = referenceObjects.OfType<DbApplicationEntity>().FirstOrDefault();
            if (dbApplication == null)
            {
                this.m_tracer.TraceWarning("Using slow cross reference of application");
                dbApplication = context.FirstOrDefault<DbApplicationEntity>(o => o.ParentKey == dbModel.VersionKey);
            }

            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    modelData.SecurityApplication = modelData.SecurityApplication.GetRelatedPersistenceService().Get(context, dbApplication.SecurityApplicationKey);
                    modelData.SetLoaded(o => o.SecurityApplication);
                    break;
            }

            return modelData.CopyObjectData(this.m_modelMapper.MapDomainInstance<DbApplicationEntity, ApplicationEntity>(dbApplication), false, declaredOnly: true);

        }
    }
}