﻿using SanteDB.Core.Model.Query;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Providers;
using System;
using System.Linq.Expressions;

namespace SanteDB.Persistence.Data.Services.Persistence
{

    /// <summary>
    /// non-generic version of ADO persistence provider
    /// </summary>
    public interface IAdoPersistenceProvider
    {

        /// <summary>
        /// Get the provider that this instance of the provider uses
        /// </summary>
        IDbProvider Provider { get; set; }

    }

    /// <summary>
    /// Represents an ADO persistence provider for <paramref name="TModel"/>
    /// </summary>
    public interface IAdoPersistenceProvider<TModel> : IAdoPersistenceProvider
    {

        /// <summary>
        /// Query for <paramref name="filter"/> on <paramref name="context"/>
        /// </summary>
        IQueryResultSet<TModel> Query(DataContext context, Expression<Func<TModel, bool>> filter);

        /// <summary>
        /// Insert the specified object into the database
        /// </summary>
        TModel Insert(DataContext context, TModel data);

        /// <summary>
        /// Update the specified object in the database context
        /// </summary>
        TModel Update(DataContext context, TModel data);

        /// <summary>
        /// Do an obsolete of the model
        /// </summary>
        TModel Obsolete(DataContext context, Guid key);

        /// <summary>
        /// Perform a get on the context
        /// </summary>
        TModel Get(DataContext context, Guid key, Guid? versionKey);
    }
}