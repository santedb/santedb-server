﻿/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2020-9-11
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Entities;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Hax
{
    /// <summary>
    /// Query builder hack 
    /// </summary>
    public class SecurityUserEntityQueryHack : IQueryBuilderHack
    {
        /// <summary>
        /// Hack the query
        /// </summary>
        public bool HackQuery(QueryBuilder builder, SqlStatement sqlStatement, SqlStatement whereClause, Type tmodel, PropertyInfo property, string queryPrefix, QueryPredicate predicate, object values, IEnumerable<TableMapping> scopedTables, params KeyValuePair<string, object>[] queryFilter)
        {
            if(typeof(SecurityUser) == tmodel && property.Name == nameof(SecurityUser.UserEntity))
            {
                var userkey = TableMapping.Get(typeof(DbUserEntity)).GetColumn(nameof(DbUserEntity.SecurityUserKey), false);
                var personSubSelect = builder.CreateQuery(typeof(UserEntity), queryFilter.Select(p => new KeyValuePair<String, Object>(p.Key.Replace("userEntity.", ""), p.Value)), null, userkey);
                var userIdKey = TableMapping.Get(typeof(DbSecurityUser)).PrimaryKey.FirstOrDefault();
                whereClause.And($"{userIdKey.Name} IN (").Append(personSubSelect).Append(")");
                return true;
            }
            return false;
        }
    }
}