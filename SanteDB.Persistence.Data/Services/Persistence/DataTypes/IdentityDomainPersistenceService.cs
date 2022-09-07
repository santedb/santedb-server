﻿using SanteDB.Core.Model;
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
    public class IdentityDomainPersistenceService : NonVersionedDataPersistenceService<IdentityDomain, DbIdentityDomain>
    {
        /// <summary>
        /// Assigning authority configuration manager
        /// </summary>
        public IdentityDomainPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override void DoDeleteReferencesInternal(DataContext context, Guid key)
        {
            context.DeleteAll<DbIdentityDomainScope>(o => o.SourceKey == key);
            context.DeleteAll<DbAssigningAuthority>(o => o.SourceKey == key);
            base.DoDeleteReferencesInternal(context, key);
        }

        /// <summary>
        /// Convert the database representation of the assigning authority
        /// </summary>
        protected override IdentityDomain DoConvertToInformationModel(DataContext context, DbIdentityDomain dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);
            retVal.AuthorityScopeXml = context.Query<DbIdentityDomainScope>(s => s.SourceKey == retVal.Key).Select(o => o.ScopeConceptKey).ToList();
            
            switch(DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.SyncLoad:
                case LoadMode.FullLoad:
                    retVal.AssigningAuthority = retVal.AssigningAuthority.GetRelatedPersistenceService().Query(context, o => o.SourceEntityKey == dbModel.Key).ToList();
                    retVal.SetLoaded(o => o.AssigningAuthority);
                    break;
            }
            return retVal;
        }

        /// <summary>
        /// Perform an insert model
        /// </summary>
        /// <param name="context">The context to be use for insertion</param>
        /// <param name="data">The data to be inserted</param>
        /// <returns>The inserted assigning authority</returns>
        protected override IdentityDomain DoInsertModel(DataContext context, IdentityDomain data)
        {
            var retVal = base.DoInsertModel(context, data);
            if (data.AuthorityScopeXml?.Any() == true)
            {
                retVal.AuthorityScopeXml = base.UpdateInternalAssociations(context, retVal.Key.Value, data.AuthorityScopeXml.Select(o => new DbIdentityDomainScope()
                {
                    ScopeConceptKey = o
                })).Select(o => o.ScopeConceptKey).ToList();
            }
            if(data.AssigningAuthority?.Any() == true)
            {
                retVal.AssigningAuthority = base.UpdateModelAssociations(context, retVal, data.AssigningAuthority).ToList();
            }
            return retVal;
        }

        /// <summary>
        /// Perform an update on the model instance
        /// </summary>
        /// <param name="context">The context on which the model should be updated</param>
        /// <param name="data">The data which is to be updated</param>
        /// <returns>The updated assigning authority</returns>
        protected override IdentityDomain DoUpdateModel(DataContext context, IdentityDomain data)
        {
            var retVal = base.DoUpdateModel(context, data); // updates the core properties
            if (data.AuthorityScopeXml != null)
            {
                retVal.AuthorityScopeXml = base.UpdateInternalAssociations(context, retVal.Key.Value,
                    data.AuthorityScopeXml?.Select(o => new DbIdentityDomainScope()
                    {
                        ScopeConceptKey = o
                    })).Select(o => o.ScopeConceptKey).ToList();
            }
            if (data.AssigningAuthority?.Any() == true)
            {
                retVal.AssigningAuthority = base.UpdateModelAssociations(context, retVal, retVal.AssigningAuthority).ToList();
            }

            return retVal;
        }
    }
}