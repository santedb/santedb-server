/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
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
