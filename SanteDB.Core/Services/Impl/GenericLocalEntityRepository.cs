using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{

    /// <summary>
    /// Represents an entity repository service.
    /// </summary>
    public class GenericLocalEntityRepository<TEntity> : GenericLocalClinicalDataRepository<TEntity>
        where TEntity : Entity
    {

        /// <summary>
        /// Find the specified act
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> query, int offset, int? count, out int totalResults, Guid queryId)
        {
            return base.Find(query, offset, count, out totalResults, queryId);
        }

        /// <summary>
        /// Find the specified act
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> query)
        {
            return base.Find(query);
        }

        /// <summary>
        /// Find the specified act
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> query, int offset, int? count, out int totalResults)
        {
            return base.Find(query, offset, count, out totalResults);
        }

        /// <summary>
        /// Find the specified act in a fast load manner
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IEnumerable<TEntity> FindFast(Expression<Func<TEntity, bool>> query, int offset, int? count, out int totalResults, Guid queryId)
        {
            return base.FindFast(query, offset, count, out totalResults, queryId);
        }

        /// <summary>
        /// Get the specified act
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.ReadClinicalData)]
        public override TEntity Get(Guid key)
        {
            return base.Get(key);
        }

        /// <summary>
        /// Get the specified act
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.ReadClinicalData)]
        public override TEntity Get(Guid key, Guid versionKey)
        {
            return base.Get(key, versionKey);
        }

        /// <summary>
        /// Insert the specified act
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.WriteClinicalData)]
        public override TEntity Insert(TEntity entity)
        {
            return base.Insert(entity);
        }

        /// <summary>
        /// Nullify (invalidate or mark entered in error) the clinical act
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.WriteClinicalData)]
        public override TEntity Nullify(Guid id)
        {
            return base.Nullify(id);
        }

        /// <summary>
        /// Obsolete (mark as no longer valid) the specified act
        /// </summary> 
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.DeleteClinicalData)]
        public override TEntity Obsolete(Guid key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Update clinical act
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PermissionPolicyIdentifiers.WriteClinicalData)]
        public override TEntity Save(TEntity data)
        {
            return base.Save(data);
        }


    }
}
