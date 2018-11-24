using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a generic resource repository factory
    /// </summary>
    public class LocalResourceRepositoryFactory : IRepositoryServiceFactory
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
