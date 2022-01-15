using SanteDB.Core.Event;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Collections
{
    /// <summary>
    /// Represents a persistence service that wraps and persists the objects in a bundle
    /// </summary>
    public class BundlePersistenceService : IDataPersistenceService<Bundle>
    {
     
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Bundle Persistence Service";

        /// <inheritdoc/>
        public event EventHandler<DataPersistedEventArgs<Bundle>> Inserted;
        /// <inheritdoc/>
        public event EventHandler<DataPersistingEventArgs<Bundle>> Inserting;
        /// <inheritdoc/>
        public event EventHandler<DataPersistedEventArgs<Bundle>> Updated;
        /// <inheritdoc/>
        public event EventHandler<DataPersistingEventArgs<Bundle>> Updating;
        /// <inheritdoc/>
        public event EventHandler<DataPersistedEventArgs<Bundle>> Obsoleted;
        /// <inheritdoc/>
        public event EventHandler<DataPersistingEventArgs<Bundle>> Obsoleting;
        /// <inheritdoc/>
        public event EventHandler<DataPersistedEventArgs<Bundle>> Deleted;
        /// <inheritdoc/>
        public event EventHandler<DataPersistingEventArgs<Bundle>> Deleting;
        /// <inheritdoc/>
        public event EventHandler<QueryResultEventArgs<Bundle>> Queried;
        /// <inheritdoc/>
        public event EventHandler<QueryRequestEventArgs<Bundle>> Querying;
        /// <inheritdoc/>
        public event EventHandler<DataRetrievingEventArgs<Bundle>> Retrieving;
        /// <inheritdoc/>
        public event EventHandler<DataRetrievedEventArgs<Bundle>> Retrieved;

        /// <inheritdoc/>
        public long Count(Expression<Func<Bundle, bool>> query, IPrincipal authContext = null)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Bundle Delete(Guid key, TransactionMode transactionMode, IPrincipal principal, DeleteMode deletionMode)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Bundle Get(Guid key, Guid? versionKey, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Bundle Insert(Bundle data, TransactionMode transactionMode, IPrincipal principal)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Bundle Obsolete(Guid key, TransactionMode transactionMode, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IQueryResultSet<Bundle> Query(Expression<Func<Bundle, bool>> query, IPrincipal principal)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IEnumerable<Bundle> Query(Expression<Func<Bundle, bool>> query, int offset, int? count, out int totalResults, IPrincipal principal, params ModelSort<Bundle>[] orderBy)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public Bundle Update(Bundle data, TransactionMode transactionMode, IPrincipal principal)
        {
            return this.Insert(data, transactionMode, principal);
        }
    }
}
