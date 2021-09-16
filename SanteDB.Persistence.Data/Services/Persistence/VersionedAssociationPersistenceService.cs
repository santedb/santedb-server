using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Abstract class for versioned associations
    /// </summary>
    public abstract class VersionedAssociationPersistenceService<TModel, TDbModel>
        : IdentifiedDataPersistenceService<TModel, TDbModel>
        where TModel : IdentifiedData, IVersionedAssociation, new()
        where TDbModel : DbIdentified, IDbVersionedAssociation, new()
    {
        /// <summary>
        /// Creates a DI instance of hte persistence layer
        /// </summary>
        public VersionedAssociationPersistenceService(IConfigurationManager configurationManager, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, adhocCacheService, dataCachingService, queryPersistence)
        {
        }

        /// <summary>
        /// Get the current version sequence for the source key
        /// </summary>
        protected abstract int GetCurrentVersionSequenceForSource(DataContext context, Guid sourceKey);

        /// <summary>
        /// Obsolete all objects
        /// </summary>
        protected override void DoObsoleteAllInternal(DataContext context, Expression<Func<TModel, bool>> expression)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            if (expression == null)
            {
                throw new ArgumentException(nameof(expression), ErrorMessages.ERR_ARGUMENT_RANGE);
            }


#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
            try
            {
#endif
                
                if (!expression.ToString().Contains(nameof(IVersionedAssociation.ObsoleteVersionSequenceId)))
                {
                    var obsoletionVersionSequenceClause = Expression.MakeMemberAccess(expression.Parameters[0], typeof(TModel).GetProperty(nameof(IVersionedAssociation.ObsoleteVersionSequenceId)));
                    expression = Expression.Lambda<Func<TModel, bool>>(Expression.And(expression.Body, Expression.MakeBinary(ExpressionType.Equal, obsoletionVersionSequenceClause, Expression.Constant(null))), expression.Parameters);
                }

                // Convert the query to a domain query so that the object persistence layer can turn the 
                // structured LINQ query into a SQL statement
                var domainQuery = context.CreateSqlStatement().SelectFrom(typeof(TDbModel));
                var domainExpression = this.m_modelMapper.MapModelExpression<TModel, TDbModel, bool>(expression, false);
                if (domainExpression != null)
                {
                    domainQuery = domainQuery.Where(domainExpression);
                }
                else
                {
                    this.m_tracer.TraceVerbose("Will use slow query construction due to complex mapped fields");
                    domainQuery = context.GetQueryBuilder(this.m_modelMapper).CreateWhere(expression);
                }

                // Get maximum source key
                var sourceKey = context.Query<TDbModel>(domainQuery).OrderByDescending(o => o.EffectiveVersionSequenceId).Select(o => o.SourceKey).FirstOrDefault();
                var sourceSequence = this.GetCurrentVersionSequenceForSource(context, sourceKey);

                context.UpdateAll(context.Query<TDbModel>(domainQuery), o =>
                {
                    o.ObsoleteVersionSequenceId = sourceSequence;
                    return o;
                });
#if DEBUG
            }
            finally
            {
                sw.Stop();
                this.m_tracer.TraceVerbose("Obsolete all {0} took {1}ms", expression, sw.ElapsedMilliseconds);
            }
#endif
        }

        /// <summary>
        /// Perform an obsoletion of the association
        /// </summary>
        protected override TDbModel DoObsoleteInternal(DataContext context, Guid key)
        {

            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if(key == default(Guid))
            {
                throw new ArgumentException(ErrorMessages.ERR_ARGUMENT_RANGE, nameof(key));
            }

            // Versioning in place? if so obsolete is update
            if (this.m_configuration.VersioningPolicy.HasFlag(Configuration.AdoVersioningPolicyFlags.AssociationVersioning))
            {
                var existing = context.FirstOrDefault<TDbModel>(o => o.Key == key);

                // Get the source table
                existing.ObsoleteVersionSequenceId = this.GetCurrentVersionSequenceForSource(context, existing.SourceKey);
                return this.DoUpdateInternal(context, existing);
            }
            else
            {
                return base.DoObsoleteInternal(context, key);
            }
        }

        /// <summary>
        /// Perform an insertion of the object
        /// </summary>
        protected override TDbModel DoInsertInternal(DataContext context, TDbModel dbModel)
        {
            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (dbModel == null)
            {
                throw new ArgumentNullException(nameof(dbModel), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // Effective seq set?
            if(dbModel.EffectiveVersionSequenceId == default(int))
            {
                dbModel.EffectiveVersionSequenceId = this.GetCurrentVersionSequenceForSource(context, dbModel.SourceKey);
            }

            return base.DoInsertInternal(context, dbModel);
        }

        /// <summary>
        /// Perform a query (appends the filter for obsolete sequence)
        /// </summary>
        protected override OrmResultSet<TDbModel> DoQueryInternal(DataContext context, Expression<Func<TModel, bool>> query, bool allowCache = false)
        {

            if(context == null)
            {
                throw new ArgumentNullException(nameof(context), ErrorMessages.ERR_ARGUMENT_NULL);
            }
            else if (query == null)
            {
                throw new ArgumentNullException(nameof(query), ErrorMessages.ERR_ARGUMENT_NULL);
            }

            // TODO: Write a utility function that looks for this
            if(!query.ToString().Contains(nameof(IVersionedAssociation.ObsoleteVersionSequenceId)))
            {
                var obsoletionVersionSequenceClause = Expression.MakeMemberAccess(query.Parameters[0], typeof(TModel).GetProperty(nameof(IVersionedAssociation.ObsoleteVersionSequenceId)));
                query = Expression.Lambda<Func<TModel, bool>>(Expression.And(query.Body, Expression.MakeBinary(ExpressionType.Equal, obsoletionVersionSequenceClause, Expression.Constant(null))), query.Parameters);
            }
            return base.DoQueryInternal(context, query, allowCache);
        }
    }
}
