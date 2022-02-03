using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Providers;
using System;
using System.Collections.Generic;
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


        /// <summary>
        /// Insert the specified object into the database
        /// </summary>
        IdentifiedData Insert(DataContext context, IdentifiedData data);

        /// <summary>
        /// Update the specified object in the database context
        /// </summary>
        IdentifiedData Update(DataContext context, IdentifiedData data);

        /// <summary>
        /// Do an obsolete of the model
        /// </summary>
        IdentifiedData Delete(DataContext context, Guid key, DeleteMode deletionMode);

        /// <summary>
        /// True if the object exists in the database
        /// </summary>
        /// <param name="context">The context to check</param>
        /// <param name="key">The primary key of the model data</param>
        /// <returns></returns>
        bool Exists(DataContext context, Guid key);

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
        new TModel Delete(DataContext context, Guid key, DeleteMode deletionMode);

        /// <summary>
        /// Perform a get on the context
        /// </summary>
        TModel Get(DataContext context, Guid key);

        /// <summary>
        /// Touch the specified object (creates a new version or updates the modified time)
        /// </summary>
        TModel Touch(DataContext context, Guid id);
    }
}