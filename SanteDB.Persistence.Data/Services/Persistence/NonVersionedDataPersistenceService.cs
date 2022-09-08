/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2022-9-7
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public NonVersionedDataPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <inheritdoc/>
        protected override bool ValidateCacheItem(TModel cacheEntry, TDbModel dataModel) => cacheEntry.UpdatedTime >= dataModel.UpdatedTime;

        /// <summary>
        /// On this class a TOUCH changes the updated time
        /// </summary>
        protected override TModel DoTouchModel(DataContext context, Guid key)
        {

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

#if DEBUG
            Stopwatch sw = new Stopwatch();
            try
            {
                sw.Start();
#endif
                // Get existing object
                var existing = context.FirstOrDefault<TDbModel>(o => o.Key == key);

                existing.UpdatedByKey = context.ContextId;
                existing.UpdatedTime = DateTimeOffset.Now;

                existing = context.Update(existing);

                return this.DoConvertToInformationModel(context, existing);
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Verbose, $"PERFORMANCE: DoTouchModel - {sw.ElapsedMilliseconds}ms", key, new StackTrace());
            }
#endif

        }

        /// <summary>
        /// Perform an update on the object and ensure that the keys
        /// </summary>
        /// <param name="context">The context on which the update is to occur</param>
        /// <param name="model">The object which should be updated in the database</param>
        /// <returns>The updated object as reflected in the database</returns>
        protected override TDbModel DoUpdateInternal(DataContext context, TDbModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            model.UpdatedByKey = context.ContextId;
            model.UpdatedTime = DateTimeOffset.Now;

            return base.DoUpdateInternal(context, model);
        }

        /// <summary>
        /// Convert the data model to information model
        /// </summary>
        protected override TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params Object[] referenceObjects)
        {
            var retVal = base.DoConvertToInformationModel(context, dbModel, referenceObjects);

            // Load strategy
            switch (DataPersistenceControlContext.Current?.LoadMode ?? this.m_configuration.LoadStrategy)
            {
                case LoadMode.FullLoad:
                    retVal.UpdatedBy = retVal.UpdatedBy.GetRelatedPersistenceService().Get(context, dbModel.UpdatedByKey.GetValueOrDefault());
                    retVal.SetLoaded(nameof(NonVersionedEntityData.UpdatedBy));
                    break;
            }

            return retVal;
        }
    }
}