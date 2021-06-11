﻿using SanteDB.Core.Model;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Providers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Represents an ADO query provider which is used by hte <see cref="AdoQueryResultSet{TData}"/>
    /// instance to calculate its result set.
    /// </summary>
    public interface IAdoQueryProvider<TModel>
    {

        /// <summary>
        /// Get the provider that this instance of the provider uses
        /// </summary>
        IDbProvider Provider { get; }

        /// <summary>
        /// Gets the instance of the query persistence service
        /// </summary>
        IQueryPersistenceService QueryPersistence { get; }

        /// <summary>
        /// Execute a query for the specified model with query identifier returning the results
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <returns>The amount of results to return</returns>
        IOrmResultSet ExecuteQuery(DataContext context, Expression<Func<TModel, bool>> query);

        /// <summary>
        /// Convert <paramref name="result"/> to model 
        /// </summary>
        TModel ToModelInstance(DataContext context, object result);

        /// <summary>
        /// Gets the specified object
        /// </summary>
        TModel Get(DataContext context, Guid key);

        /// <summary>
        /// Map sort expression
        /// </summary>
        /// <param name="sortExpression">The sort expression to map</param>
        /// <returns>The mapped sort expression</returns>
        Expression MapSortExpression(Expression<Func<TModel, dynamic>> sortExpression);
    }
}
