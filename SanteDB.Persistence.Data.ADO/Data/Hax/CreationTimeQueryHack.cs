/*
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
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Map;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SanteDB.Persistence.Data.ADO.Data.Hax
{
    /// <summary>
    /// Represents a hack for creation time that will correct to the creation time of the first version
    /// </summary>
    /// <remarks>
    /// Without this query hack a query for creationTime will simply filter on the current tuple's "creationTime" which is actually the time that particular
    /// version of the record was created. This hack will add a filter which will place an additional constraint when creationTime is used as a query parameter
    /// on a versioned entity, specifying that the "creationTime" be on the first version (with no replacement id).
    /// 
    /// "modifiedOn" query parameters should still resolve to regular "creationTime" parameters as "modifiedOn" is seeking the newest version of a resource.
    /// </remarks>
    public class CreationTimeQueryHack : IQueryBuilderHack
    {
        // The mapper
        private ModelMapper m_mapper;

        /// <summary>
        /// CTOR taking mapper as parm
        /// </summary>
        public CreationTimeQueryHack(ModelMapper map)
        {
            this.m_mapper = map;
        }

        /// <summary>
        /// Query hack for creation time
        /// </summary>
        public bool HackQuery(QueryBuilder builder, SqlStatement sqlStatement, SqlStatement whereClause, Type tmodel, PropertyInfo property, string queryPrefix, QueryPredicate predicate, object values, IEnumerable<TableMapping> scopedTables, params KeyValuePair<String, object>[] queryFilter)
        {
            if(property.Name == nameof(IBaseEntityData.CreationTime) && typeof(IVersionedEntity).IsAssignableFrom(tmodel)) // filter by first creation time
            {
                var ormMap = scopedTables.SelectMany(o => o.Columns);
                var replacesVersion = ormMap.FirstOrDefault(o => o.SourceProperty.Name == nameof(IDbVersionedData.ReplacesVersionKey));
                // Find the join Column
                var joinCol = replacesVersion.Table.Columns.FirstOrDefault(o => o.ForeignKey?.Table == scopedTables.Last().OrmType);

                whereClause.And($"EXISTS (SELECT crt{replacesVersion.Table.TableName}.{joinCol.Name} FROM {replacesVersion.Table.TableName} AS crt{replacesVersion.Table.TableName} WHERE crt{replacesVersion.Table.TableName}.{joinCol.Name} = {queryPrefix}{replacesVersion.Table.TableName}.{joinCol.Name} AND crt{replacesVersion.Table.TableName}.{replacesVersion.Name} IS NULL ");
                whereClause.And(builder.CreateWhereCondition(tmodel, predicate.Path, values, "crt", scopedTables.ToList()));
                whereClause.Append(")");
                return true;
            }
            return false;
        }
    }
}
