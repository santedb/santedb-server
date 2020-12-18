using SanteDB.Core.Model.Map;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a persistence settings provider which are used by the persistence services to 
    /// load mappers / configuration etc.
    /// </summary>
    public interface IAdoPersistenceSettingsProvider
    {

        /// <summary>
        /// Get configuration
        /// </summary>
        AdoPersistenceConfigurationSection GetConfiguration();

        /// <summary>
        /// Gets the mode mapper
        /// </summary>
        /// <returns></returns>
        ModelMapper GetMapper();

        /// <summary>
        /// Get query builder
        /// </summary>
        /// <returns></returns>
        QueryBuilder GetQueryBuilder();

        /// <summary>
        /// Get Persister
        /// </summary>
        /// <param name="tDomain"></param>
        /// <returns></returns>
        IAdoPersistenceService GetPersister(Type tDomain);
    }
}
