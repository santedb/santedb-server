﻿using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// A class which persists non-versioned data (which has updated timestamps)
    /// </summary>
    /// <typeparam name="TModel">The type of model to be persisted</typeparam>
    /// <typeparam name="TDbModel">The non-versioned physical model data</typeparam>
    public abstract class NonVersionedDataPersistenceService<TModel, TDbModel> : BaseEntityDataPersistenceService<TModel, TDbModel>
        where TModel : NonVersionedEntityData, new()
        where TDbModel : DbNonVersionedBaseData, new()
    {

        /// <summary>
        /// Creates a new instance of the non-versioned persistence service with specified DI services
        /// </summary>
        public NonVersionedDataPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }


        /// <summary>
        /// Perform an update on the object and ensure that the keys
        /// </summary>
        /// <param name="context">The context on which the update is to occur</param>
        /// <param name="model">The object which should be updated in the database</param>
        /// <returns>The updated object as reflected in the database</returns>
        protected override TDbModel DoUpdateInternal(DataContext context, TDbModel model)
        {
            if(model == null)
            {
                throw new ArgumentNullException(nameof(model), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            model.UpdatedByKey = context.ContextId;
            model.UpdatedTime = DateTimeOffset.Now;

            return base.DoUpdateInternal(context, model);
        }

    }
}
