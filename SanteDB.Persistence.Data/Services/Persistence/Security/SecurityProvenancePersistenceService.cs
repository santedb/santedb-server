using SanteDB.Core.i18n;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Security
{
    /// <summary>
    /// Security provenance persistence service
    /// </summary>
    public class SecurityProvenancePersistenceService : IdentifiedDataPersistenceService<SecurityProvenance, DbSecurityProvenance>
    {
        /// <summary>
        /// Creates a new persistence service for security provenance
        /// </summary>
        public SecurityProvenancePersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Update provenance not supported
        /// </summary>
        protected override DbSecurityProvenance DoUpdateInternal(DataContext context, DbSecurityProvenance model)
        {
            throw new NotSupportedException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_PERMITTED));
        }

        /// <summary>
        /// Obsoletion of provenance not supported
        /// </summary>
        protected override DbSecurityProvenance DoDeleteInternal(DataContext context, Guid key, DeleteMode deletionMode)
        {
            throw new NotSupportedException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_PERMITTED));
        }

        /// <summary>
        /// Obsoletion of provenance not supported
        /// </summary>
        protected override IEnumerable<DbSecurityProvenance> DoDeleteAllInternal(DataContext context, Expression<Func<SecurityProvenance, bool>> expression, DeleteMode deleteMode)
        {
            // The user may be trying to purge old provenance objects
            if (deleteMode == DeleteMode.PermanentDelete) // this statement will fail due to RI in the database anyways - so just send it
            {
                return base.DoDeleteAllInternal(context, expression, deleteMode);
            }
            else
            {
                throw new NotSupportedException(this.m_localizationService.GetString(ErrorMessageStrings.NOT_PERMITTED));
            }
        }
    }
}