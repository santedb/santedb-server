using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a basic ADO FreeText search service which really just filters on database tables
    /// </summary>
    public class AdoFreetextSearchService : IFreetextSearchService
    {
        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string ServiceName => "Basic ADO.NET _any Search Service";

        /// <summary>
        /// Search for the specified object in the list of terms
        /// </summary>
        public IEnumerable<TEntity> Search<TEntity>(string[] term, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TEntity>[] orderBy) where TEntity : IdentifiedData, new()
        {
            var idps = ApplicationServiceContext.Current.GetService<IUnionQueryDataPersistenceService<TEntity>>();
            if (idps == null)
                throw new InvalidOperationException("Cannot find a UNION query repository service");

            // Perform the queries on the terms
            var searchFilters = new List<Expression<Func<TEntity, bool>>>(term.Length);
            searchFilters.Add(QueryExpressionParser.BuildLinqExpression<TEntity>(new NameValueCollection() { { "name.component.value", term } }));
            searchFilters.Add(QueryExpressionParser.BuildLinqExpression<TEntity>(new NameValueCollection() { { "identifier.value", term } }));

            // Send query
            return idps.Union(searchFilters.ToArray(), queryId, offset, count, out totalResults, AuthenticationContext.Current.Principal, orderBy);
        }
    }
}
