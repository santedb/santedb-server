using SanteDB.Core.Model;
using System.Diagnostics;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a generic resource repository factory
    /// </summary>
    [ServiceProvider("Local Data Repository Factory")]
    public class LocalRepositoryFactoryService : IRepositoryServiceFactory
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Data Repository Factory";

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
