/*
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
 * Date: 2019-11-27
 */

using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Map;
using SanteDB.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SanteDB.Persistence.Data.Hax
{
    /// <summary>
    /// Provides a much faster way of search by address and name on entities
    /// </summary>
    public class EntityAddressNameQueryHack : IQueryBuilderHack
    {
        /// <summary>
        /// Hack the query
        /// </summary>
        public bool HackQuery(QueryBuilder builder, SqlStatement sqlStatement, SqlStatement whereClause, Type tmodel, PropertyInfo property, string queryPrefix, QueryPredicate predicate, object values, IEnumerable<TableMapping> scopedTables, params KeyValuePair<String, object>[] queryFilter)
        {
            String cmpTblType = String.Empty, keyName = String.Empty;
            Type guardType = null, componentType = null;
            // We can attempt to hack the address
            if (typeof(EntityAddress).IsAssignableFrom(tmodel))
            {
                cmpTblType = "ent_addr_cmp_tbl";
                guardType = typeof(AddressComponentKeys);
                componentType = typeof(EntityAddressComponent);
                keyName = "addr_id";
            }
            else if (typeof(EntityName).IsAssignableFrom(tmodel))
            {
                cmpTblType = "ent_name_cmp_tbl";
                guardType = typeof(NameComponentKeys);
                componentType = typeof(EntityNameComponent);
                keyName = "name_id";
            }
            else
                return false;

            // Not applicable for us if
            //  - Not a name or address
            //  - Predicate is not component.value
            //  - There is already other where clause stuff
            if (guardType == null ||
                predicate.Path != "component" ||
                predicate.SubPath != "value" ||
                !String.IsNullOrEmpty(whereClause.SQL))
                return false;

            // Pop the last statement off
            // var fromClause = sqlStatement.RemoveLast();

            var subQueryAlias = $"{queryPrefix}{scopedTables.First().TableName}";

            whereClause.And($"{subQueryAlias}.{keyName} IN (");

            foreach (var itm in queryFilter)
            {
                var pred = QueryPredicate.Parse(itm.Key);
                String guardFilter = String.Empty;

                // Do we have a guard for address?
                if (!String.IsNullOrEmpty(pred.Guard))
                {
                    // Translate Guards to UUIDs
                    var guards = pred.Guard.Split('|');
                    for (int i = 0; i < guards.Length; i++)
                        if (!Guid.TryParse(guards[i], out Guid _))
                        {
                            guards[i] = guardType.GetField(guards[i]).GetValue(null).ToString();
                        }
                    if (guards.Any(o => o == null)) return false;

                    // Add to where clause
                    guardFilter = $"AND {queryPrefix}{cmpTblType}.typ_cd_id IN ({String.Join(",", guards.Select(o => $"'{o}'"))})";
                }

                // Filter on the component value
                var value = itm.Value;
                if (value is String)
                    value = new List<Object>() { value };
                var qValues = value as List<Object>;

                // Filter based on type and prefix :)
                whereClause
                        .Append($" SELECT {queryPrefix}{cmpTblType}.{keyName} ")
                            .Append($" FROM {cmpTblType} AS {queryPrefix}{cmpTblType} ")
                            .Append(" WHERE ")
                            .Append(builder.CreateSqlPredicate($"{queryPrefix}{cmpTblType}", "val", componentType.GetProperty("Value"), qValues))
                            .Append(guardFilter)
                            .Append(" INTERSECT ");
            }
            whereClause.RemoveLast();
            whereClause.Append($") ");
            whereClause.And($"{subQueryAlias}.obslt_vrsn_seq_id IS NULL");

            return true;
        }
    }
}