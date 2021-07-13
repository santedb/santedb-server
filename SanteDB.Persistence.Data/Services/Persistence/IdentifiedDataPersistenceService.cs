using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// This persistence class represents a persistence service which is capable of storing and maintaining 
    /// an IdentifiedData instance and its equivalent IDbIdentified
    /// </summary>
    public class IdentifiedDataPersistenceService<TModel, TDbModel>
        : BasePersistenceService<TModel, TDbModel>
        where TModel : IdentifiedData, new()
        where TDbModel : DbIdentified, new()
    {
        /// <summary>
        /// Convert <paramref name="model" /> to a <typeparamref name="TDbModel"/>
        /// </summary>
        /// <param name="context">The data context in case data access is required</param>
        /// <param name="model">The model to be converted</param>
        /// <param name="referenceObjects">The referenced objects (for reference)</param>
        /// <returns>The <typeparamref name="TDbModel"/> instance</returns>
        protected override TDbModel DoConvertToDataModel(DataContext context, TModel model, params IDbIdentified[] referenceObjects)
        {
            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(model == default(TModel))
            {
                throw new ArgumentNullException(nameof(model), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            return this.m_modelMapper.MapModelInstance<TModel, TDbModel>(model);
        }

        /// <summary>
        /// Converts an information model <paramref name="dbModel"/> to <typeparamref name="TModel"/>
        /// </summary>
        /// <param name="context">The data context which is being converted on</param>
        /// <param name="dbModel">The database model to be converted</param>
        /// <param name="referenceObjects">The reference objects for lookup</param>
        /// <returns>The converted model</returns>
        protected override TModel DoConvertToInformationModel(DataContext context, TDbModel dbModel, params IDbIdentified[] referenceObjects)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (dbModel == default(TDbModel))
            {
                throw new ArgumentNullException(nameof(dbModel), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            return this.m_modelMapper.MapDomainInstance<TDbModel, TModel>(dbModel);
        }

        /// <summary>
        /// Get a database model version direct from the database
        /// </summary>
        /// <param name="context">The context from which the data should be fetched</param>
        /// <param name="key">The key of data which should be fetched</param>
        /// <param name="versionKey">The version key</param>
        /// <param name="allowCache">True if loading data from the ad-hoc caching service is allowed</param>
        /// <returns>The database model</returns>
        protected override TDbModel DoGetInternal(DataContext context, Guid key, Guid? versionKey, bool allowCache = false)
        {
            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            TDbModel retVal = default(TDbModel);
            var cacheKey = this.GetAdHocCacheKey(key);
            if(allowCache && (this.m_configuration?.CachingPolicy.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
            {
                retVal = this.m_adhocCache?.Get<TDbModel>(cacheKey);
            }
            if(retVal == null)
            {
                retVal = context.FirstOrDefault<TDbModel>(o => o.Key == key);

                if ((this.m_configuration?.CachingPolicy.Targets & Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects) == Data.Configuration.AdoDataCachingPolicyTarget.DatabaseObjects)
                {
                    this.m_adhocCache.Add<TDbModel>(cacheKey, retVal, this.m_configuration.CachingPolicy?.DataObjectExpiry);
                }
            }

            return retVal;
        }

        /// <summary>
        /// Perform an insert of an identified object
        /// </summary>
        protected override TDbModel DoInsertInternal(DataContext context, TDbModel dbModel)
        {
            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            return context.Insert(dbModel);
        }

        /// <summary>
        /// Obsolete the specified object
        /// </summary>
        protected override TDbModel DoObsoleteInternal(DataContext context, Guid key)
        {
            throw new NotImplementedException();
        }

        protected override OrmResultSet<TDbModel> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, bool allowCache = false)
        {
            throw new NotImplementedException();
        }

        protected override TDbModel DoUpdateInternal(DataContext context, TDbModel model)
        {
            throw new NotImplementedException();
        }
    }
}
