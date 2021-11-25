using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using SanteDB.Persistence.Data.Model.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.DataTypes
{
    /// <summary>
    /// Assigning authority persistence service
    /// </summary>
    public class AssigningAuthorityPersistenceService : NonVersionedDataPersistenceService<AssigningAuthority, DbAssigningAuthority>
    {
        /// <summary>
        /// Assigning authority configuration manager
        /// </summary>
        public AssigningAuthorityPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Convert the database representation of the assigning authority
        /// </summary>
        protected override AssigningAuthority DoConvertToInformationModel(DataContext context, DbAssigningAuthority dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            retVal.AuthorityScopeXml = context.Query<DbAuthorityScope>(s => s.SourceKey == retVal.Key).Select(o => o.ScopeConceptKey).ToList();
            return retVal;
        }

        /// <summary>
        /// Perform an insert model
        /// </summary>
        /// <param name="context">The context to be use for insertion</param>
        /// <param name="data">The data to be inserted</param>
        /// <returns>The inserted assigning authority</returns>
        protected override AssigningAuthority DoInsertModel(DataContext context, AssigningAuthority data)
        {
            var retVal = base.DoInsertModel(context, data);
            if (data.AuthorityScopeXml?.Any() == true)
            {
                retVal.AuthorityScopeXml = base.UpdateInternalAssociations(context, retVal.Key.Value, data.AuthorityScopeXml.Select(o => new DbAuthorityScope()
                {
                    ScopeConceptKey = o
                })).Select(o => o.ScopeConceptKey).ToList();
            }
            return retVal;
        }

        /// <summary>
        /// Perform an update on the model instance
        /// </summary>
        /// <param name="context">The context on which the model should be updated</param>
        /// <param name="data">The data which is to be updated</param>
        /// <returns>The updated assigning authority</returns>
        protected override AssigningAuthority DoUpdateModel(DataContext context, AssigningAuthority data)
        {
            var retVal = base.DoUpdateModel(context, data); // updates the core properties
            if (data.AuthorityScopeXml != null)
            {
                retVal.AuthorityScopeXml = base.UpdateInternalAssociations(context, retVal.Key.Value,
                    data.AuthorityScopeXml?.Select(o => new DbAuthorityScope()
                    {
                        ScopeConceptKey = o
                    })).Select(o => o.ScopeConceptKey).ToList();
            }

            return retVal;
        }
    }
}