using SanteDB.OrmLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Index
{
    /// <summary>
    /// Query filter hack which uses the ADO query filter manager
    /// </summary>
    public class AdoPreparedFilterQueryHack : IQueryBuilderHack
    {
        public bool HackQuery(QueryBuilder builder, SqlStatement sqlStatement, SqlStatement whereClause, Type tmodel, PropertyInfo property, string queryPrefix, QueryPredicate predicate, object values, IEnumerable<TableMapping> scopedTables, params KeyValuePair<string, object>[] queryFilter)
        {
            throw new NotImplementedException();
        }
    }
}
