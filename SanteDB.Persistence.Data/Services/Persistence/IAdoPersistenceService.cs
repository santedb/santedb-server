using SanteDB.Core.Services;
using SanteDB.OrmLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// ADO Persistence Service
    /// </summary>
    public interface IAdoPersistenceService : IDataPersistenceService
    {
        /// <summary>
        /// Inserts the specified object
        /// </summary>
        Object Insert(DataContext context, Object data);

        /// <summary>
        /// Updates the specified data
        /// </summary>
        Object Update(DataContext context, Object data);

        /// <summary>
        /// Obsoletes the specified data
        /// </summary>
        Object Obsolete(DataContext context, Object data);

        /// <summary>
        /// Gets the specified data
        /// </summary>
        Object Get(DataContext context, Guid id);

        /// <summary>
        /// Map to model instance
        /// </summary>
        Object ToModelInstance(object domainInstance, DataContext context);

        /// <summary>
        /// Returns true if the specified object exists
        /// </summary>
        bool Exists(DataContext context, Guid id);
    }
}
