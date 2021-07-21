using SanteDB.OrmLite.Providers;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Represents an ADO persistence provider
    /// </summary>
    public interface IAdoPersistenceProvider
    {

        /// <summary>
        /// Get the provider that this instance of the provider uses
        /// </summary>
        IDbProvider Provider { get; set;  }

    }
}