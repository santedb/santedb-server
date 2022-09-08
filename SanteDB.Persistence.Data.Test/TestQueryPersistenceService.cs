/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2022-9-7
 */
using NUnit.Framework;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Implement query persistence serivce
    /// </summary>
    [ExcludeFromCodeCoverage]
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
            if (this.m_queryResults.TryGetValue(queryId, out List<Guid> res))
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
            if (this.m_queryResults.TryGetValue(queryId, out List<Guid> res))
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
            if (this.m_queryResults.TryGetValue(queryId, out List<Guid> res))
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
            if (this.m_expectedQueryId != Guid.Empty)
            {
                Assert.AreEqual(this.m_expectedQueryId, queryId);
                this.m_expectedQueryId = Guid.Empty;
            }
            if (this.m_expectedResults != default(int))
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
        public void SetQueryTag(Guid queryId, object value)
        { }

        public void AbortQuerySet(Guid queryId)
        {
        }
    }
}