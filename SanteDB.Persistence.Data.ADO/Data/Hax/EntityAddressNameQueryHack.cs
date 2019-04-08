﻿/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SanteDB.Persistence.Data.ADO.Data.Hax
{
    /// <summary>
    /// Provides a much faster way of search by address and name on entities
    /// </summary>
    public class EntityAddressNameQueryHack : IQueryBuilderHack
    {
        /// <summary>
        /// Hack the query 
        /// </summary>
        public bool HackQuery(QueryBuilder builder, SqlStatement sqlStatement, SqlStatement whereClause, Type tmodel, PropertyInfo property, string queryPrefix, QueryPredicate predicate, object values, IEnumerable<TableMapping> scopedTables)
        {

            String cmpTblType = String.Empty, valTblType = String.Empty;
            Type guardType = null, componentType = null;
            // We can attempt to hack the address
            if (typeof(EntityAddress).IsAssignableFrom(tmodel))
            {
                cmpTblType = "ent_addr_cmp_tbl";
                valTblType = "ent_addr_cmp_val_tbl";
                guardType = typeof(AddressComponentKeys);
                componentType = typeof(EntityAddressComponent);
                if (!sqlStatement.Build().SQL.Contains("INNER JOIN ent_addr_cmp_tbl"))
                    sqlStatement.Append($" INNER JOIN ent_addr_cmp_tbl AS {queryPrefix}ent_addr_cmp_tbl ON ({queryPrefix}ent_addr_cmp_tbl.addr_id = {queryPrefix}ent_addr_tbl.addr_id) ");
            }
            else if (typeof(EntityName).IsAssignableFrom(tmodel))
            {
                cmpTblType = "ent_name_cmp_tbl";
                valTblType = "phon_val_tbl";
                guardType = typeof(NameComponentKeys);
                componentType = typeof(EntityNameComponent);
                if(!sqlStatement.Build().SQL.Contains("INNER JOIN ent_name_cmp_tbl"))
                    sqlStatement.Append($" INNER JOIN ent_name_cmp_tbl AS {queryPrefix}ent_name_cmp_tbl ON ({queryPrefix}ent_name_cmp_tbl.name_id = {queryPrefix}ent_name_tbl.name_id) ");
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

            // Do we have a guard for address?
            if (!String.IsNullOrEmpty(predicate.Guard))
            {
                // Translate Guards to UUIDs
                var guards = predicate.Guard.Split('|');
                for (int i = 0; i < guards.Length; i++)
                    guards[i] = guardType.GetField(guards[i]).GetValue(null).ToString();
                if (guards.Any(o => o == null)) return false;

                // Add to where clause
                whereClause.And($"{queryPrefix}{cmpTblType}.typ_cd_id IN ({String.Join(",", guards.Select(o => $"'{o}'"))})");
            }

            // Filter on the component value
            if (values is String)
                values = new List<Object>() { values };
            var qValues = values as List<Object>;

            sqlStatement.Append($"INNER JOIN (SELECT val_seq_id FROM {valTblType} AS {queryPrefix}{valTblType} WHERE ");
            sqlStatement.Append(builder.CreateSqlPredicate($"{queryPrefix}{valTblType}", "val", componentType.GetProperty("Value"), qValues));
            sqlStatement.Append($") AS {queryPrefix}{valTblType} ON ({queryPrefix}{valTblType}.val_seq_id = {queryPrefix}{cmpTblType}.val_seq_id)");
            return true;
        }
    }
}
