using SanteDB.Core.Model;
using System.Diagnostics;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a generic resource repository factory
    /// </summary>
    public class LocalRepositoryFactoryService : IRepositoryServiceFactory
    {
        /// <summary>
        /// Create the specified resource service factory
        /// </summary>
        public IRepositoryService<T> CreateRepository<T>() where T : IdentifiedData
        {
            new TraceSource(SanteDBConstants.DataTraceSourceName).TraceEvent(TraceEventType.Warning, 666, "Creating generic repository for {0}. Security may be compromised! Please register an appropriate repository service with the host", typeof(T).FullName);
            return new GenericLocalRepository<T>();
        }

    }
}
