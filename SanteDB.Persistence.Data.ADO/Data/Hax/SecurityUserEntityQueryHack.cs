using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Data.Hax
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
                var personSubSelect = builder.CreateQuery<UserEntity>(queryFilter.Select(p => new KeyValuePair<String, Object>(p.Key.Replace("userEntity.", ""), p.Value)), null, userkey);
                var userIdKey = TableMapping.Get(typeof(DbSecurityUser)).PrimaryKey.FirstOrDefault();
                whereClause.And($"{userIdKey.Name} IN (").Append(personSubSelect).Append(")");
                return true;
            }
            return false;
        }
    }
}
