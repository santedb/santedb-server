using NUnit.Framework;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Implement query persistence serivce
    /// </summary>
    public class TestQueryPersistenceService : IQueryPersistenceService
    {


        private Guid m_expectedQueryId;

        private int m_expectedResults;

        private Dictionary<Guid, List<Guid>> m_queryResults = new Dictionary<Guid, List<Guid>>();

        /// <summary>
        /// Expected query stats
        /// </summary>
        public void SetExpectedQueryStats(Guid queryId, int expectedResults)
        {
            this.m_expectedQueryId = queryId;
            this.m_expectedResults = expectedResults;
        }

        /// <summary>
        /// Service name
        /// </summary>
        public string ServiceName => "Dummy Query Persistence Service";

        /// <summary>
        /// Add results to the result set
        /// </summary>
        public void AddResults(Guid queryId, IEnumerable<Guid> results, int totalResults)
        {
            if(this.m_queryResults.TryGetValue(queryId, out List<Guid> res))
            {
                res.AddRange(results);
            }
        }

        /// <summary>
        /// Find query id by tag
        /// </summary>
        public Guid FindQueryId(object queryTag)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get query results
        /// </summary>
        public IEnumerable<Guid> GetQueryResults(Guid queryId, int offset, int count)
        {
            if(this.m_queryResults.TryGetValue(queryId, out List<Guid> res))
            {
                return res.Skip(offset).Take(count);
            }
            return new Guid[0];
        }

        /// <summary>
        /// Get query tag
        /// </summary>
        public object GetQueryTag(Guid queryId)
        {
            return null;
        }

        /// <summary>
        /// Is the query registered
        /// </summary>
        public bool IsRegistered(Guid queryId)
        {
            return this.m_queryResults.ContainsKey(queryId);
        }

        /// <summary>
        /// Get the total results
        /// </summary>
        public long QueryResultTotalQuantity(Guid queryId)
        {
            if(this.m_queryResults.TryGetValue(queryId, out List<Guid> res))
            {
                return res.LongCount();
            }
            return 0;
        }

        /// <summary>
        /// Register a query set
        /// </summary>
        public bool RegisterQuerySet(Guid queryId, IEnumerable<Guid> results, object tag, int totalResults)
        {
            if(this.m_expectedQueryId != Guid.Empty)
            {
                Assert.AreEqual(this.m_expectedQueryId, queryId);
                this.m_expectedQueryId = Guid.Empty;
            }
            if(this.m_expectedResults != default(int))
            {
                Assert.AreEqual(this.m_expectedResults, totalResults);
                this.m_expectedResults = 0;
            }
            this.m_queryResults.Add(queryId, new List<Guid>(results));
            return true;
        }

        /// <summary>
        /// Set query tag
        /// </summary>
        public void SetQueryTag(Guid queryId, object value) { }
    }
}
