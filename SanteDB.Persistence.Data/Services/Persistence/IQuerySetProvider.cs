using SanteDB.Core.Model.Query;
using SanteDB.OrmLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Represents a class which can do query set creation
    /// </summary>
    internal interface IQuerySetProvider : IAdoPersistenceProvider
    {

        /// <summary>
        /// Execute <paramref name="query"/> directly and return the appropriate result set
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <returns>The proper query result</returns>
        IQueryResultSet Query(SqlStatement query);

    }
}
