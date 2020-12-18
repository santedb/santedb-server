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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.ADO.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{

    /// <summary>
    /// Security provenance service
    /// </summary>
    public class SecurityProvenancePersistenceService : IdentifiedPersistenceService<SecurityProvenance, DbSecurityProvenance>
    {

        public SecurityProvenancePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Inserting with IDataContext is not permitted
        /// </summary>
        public override SecurityProvenance InsertInternal(DataContext context, SecurityProvenance data)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Updating Provenance with DataPersistenceService is not permitted
        /// </summary>
        public override SecurityProvenance UpdateInternal(DataContext context, SecurityProvenance data)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Obsoleting provenance is not supported
        /// </summary>
        public override SecurityProvenance ObsoleteInternal(DataContext context, SecurityProvenance data)
        {
            throw new NotSupportedException();
        }

       
    }
	/// <summary>
	/// Security user persistence
	/// </summary>
	public class SecurityApplicationPersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityApplication, DbSecurityApplication>
	{

        public SecurityApplicationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<SecurityApplication>[] orderBy)
        {
            if (orderBy == null || orderBy.Length == 0)
                orderBy = new ModelSort<SecurityApplication>[]
                {
                    new ModelSort<SecurityApplication>(o=>o.CreationTime, Core.Model.Map.SortOrderType.OrderByDescending)
                };
            return base.AppendOrderBy(rawQuery, orderBy);
        }

        internal override SecurityApplication Get(DataContext context, Guid key)
		{
			var application = base.Get(context, key);

			if (application != null && context.LoadState == LoadState.FullLoad)
			{
				var policyQuery = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom(typeof(DbSecurityApplicationPolicy), typeof(DbSecurityPolicy))
										.InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
										.Where<DbSecurityApplicationPolicy>(o => o.SourceKey == application.Key);
                var mapper = this.m_settingsProvider.GetMapper();
                application.Policies = context.Query<CompositeResult<DbSecurityApplicationPolicy, DbSecurityPolicy>>(policyQuery).ToArray().Select(o => new SecurityPolicyInstance(mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
			}

			return application;
		}

		/// <summary>
		/// Insert the specified object
		/// </summary>
		public override Core.Model.Security.SecurityApplication InsertInternal(DataContext context, Core.Model.Security.SecurityApplication data)
		{
			var retVal = base.InsertInternal(context, data);

			if (data.Policies == null)
				return retVal;

            data.Policies.ForEach(o => o.PolicyKey = o.Policy?.EnsureExists(context)?.Key ?? o.PolicyKey);
            foreach (var itm in data.Policies.Select(o => new DbSecurityApplicationPolicy()
			{
				PolicyKey = o.PolicyKey.Value,
				GrantType = (int)o.GrantType,
				SourceKey = retVal.Key.Value,
				Key = Guid.NewGuid()
			}))
				context.Insert(itm);

            retVal.ApplicationSecret = null;

            return retVal;
		}

		/// <summary>
		/// Represent as model instance
		/// </summary>
		public override Core.Model.Security.SecurityApplication ToModelInstance(object dataInstance, DataContext context)
		{
			var retVal = base.ToModelInstance(dataInstance, context);
			if (retVal == null) return null;
			var policyQuery = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom(typeof(DbSecurityApplicationPolicy), typeof(DbSecurityPolicy))
				.InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
				.Where<DbSecurityApplicationPolicy>(o => o.SourceKey == retVal.Key);

            var mapper = this.m_settingsProvider.GetMapper();
            retVal.Policies = context.Query<CompositeResult<DbSecurityApplicationPolicy, DbSecurityPolicy>>(policyQuery).ToArray().Select(o => new SecurityPolicyInstance(mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
			return retVal;
		}

		/// <summary>
		/// Updates a security application.
		/// </summary>
		/// <param name="context">The data context.</param>
		/// <param name="data">The security application to update.</param>
		/// <param name="principal">The authentication principal.</param>
		/// <returns>Returns the updated security application.</returns>
		public override Core.Model.Security.SecurityApplication UpdateInternal(DataContext context, Core.Model.Security.SecurityApplication data)
		{
            var instance = base.UpdateInternal(context, data);

			if (data.Policies != null && data.Policies.Count > 0)
			{
				context.Delete<DbSecurityApplicationPolicy>(o => o.SourceKey == data.Key);
                data.Policies.ForEach(o => o.PolicyKey = o.Policy?.EnsureExists(context)?.Key ?? o.PolicyKey);
                foreach (var pol in data.Policies.Select(o => new DbSecurityApplicationPolicy
				{
					PolicyKey = o.PolicyKey.Value,
					GrantType = (int)o.GrantType,
					SourceKey = data.Key.Value,
					Key = Guid.NewGuid()
				}))
					context.Insert(pol);
			}

            data.ApplicationSecret = null;
			return data;
		}
	}

	/// <summary>
	/// Security user persistence
	/// </summary>
	public class SecurityDevicePersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityDevice, DbSecurityDevice>
	{
        public SecurityDevicePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<SecurityDevice>[] orderBy)
        {
            if (orderBy == null || orderBy.Length == 0)
                orderBy = new ModelSort<SecurityDevice>[]
                {
                    new ModelSort<SecurityDevice>(o=>o.UpdatedTime, Core.Model.Map.SortOrderType.OrderByDescending),
                    new ModelSort<SecurityDevice>(o=>o.CreationTime, Core.Model.Map.SortOrderType.OrderByDescending)
                };
            return base.AppendOrderBy(rawQuery, orderBy);
        }

        internal override SecurityDevice Get(DataContext context, Guid key)
		{
			var device = base.Get(context, key);

			if (device != null && context.LoadState == LoadState.FullLoad)
			{
				var policyQuery = context.CreateSqlStatement<DbSecurityDevicePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityDevicePolicy))
										.InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
										.Where<DbSecurityDevicePolicy>(o => o.SourceKey == device.Key);
                var mapper = this.m_settingsProvider.GetMapper();
                device.Policies = context.Query<CompositeResult<DbSecurityDevicePolicy, DbSecurityPolicy>>(policyQuery).ToArray().Select(o => new SecurityPolicyInstance(mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
			}

			return device;
		}

		/// <summary>
		/// Insert the specified object
		/// </summary>
		public override Core.Model.Security.SecurityDevice InsertInternal(DataContext context, Core.Model.Security.SecurityDevice data)
		{
			var retVal = base.InsertInternal(context, data);

			if (data.Policies == null)
				return retVal;

            data.Policies.ForEach(o => o.PolicyKey = o.Policy?.EnsureExists(context)?.Key ?? o.PolicyKey);
			foreach (var itm in data.Policies.Select(o => new DbSecurityDevicePolicy()
			{
				PolicyKey = o.PolicyKey.Value,
				GrantType = (int)o.GrantType,
				SourceKey = retVal.Key.Value,
				Key = Guid.NewGuid()
			}))
				context.Insert(itm);

            data.DeviceSecret = null;

            return retVal;
		}

		/// <summary>
		/// Represent as model instance
		/// </summary>
		public override Core.Model.Security.SecurityDevice ToModelInstance(object dataInstance, DataContext context)
		{
			var retVal = base.ToModelInstance(dataInstance, context);
			if (retVal == null) return null;
			var policyQuery = context.CreateSqlStatement<DbSecurityDevicePolicy>().SelectFrom(typeof(DbSecurityDevicePolicy), typeof(DbSecurityPolicy))
				.InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
				.Where<DbSecurityDevicePolicy>(o => o.SourceKey == retVal.Key);

            var mapper = this.m_settingsProvider.GetMapper();
            retVal.Policies = context.Query<CompositeResult<DbSecurityDevicePolicy, DbSecurityPolicy>>(policyQuery).ToArray().Select(o => new SecurityPolicyInstance(mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
			return retVal;
		}

		/// <summary>
		/// Update the roles to security user
		/// </summary>
		public override Core.Model.Security.SecurityDevice UpdateInternal(DataContext context, Core.Model.Security.SecurityDevice data)
		{
            var retVal = base.UpdateInternal(context, data);

            if (data.Policies != null && data.Policies.Count > 0)
			{
				context.Delete<DbSecurityDevicePolicy>(o => o.SourceKey == data.Key);
                data.Policies.ForEach(o => o.PolicyKey = o.Policy?.EnsureExists(context)?.Key ?? o.PolicyKey);
                foreach (var pol in data.Policies.Select(o => new DbSecurityDevicePolicy
				{
					PolicyKey = o.PolicyKey.Value,
					GrantType = (int)o.GrantType,
					SourceKey = data.Key.Value,
					Key = Guid.NewGuid()
				}))
					context.Insert(pol);
			}

            data.DeviceSecret = null;

            return data;
		}
	}

	/// <summary>
	/// Represents a persistence service for security policies.
	/// </summary>
	public class SecurityPolicyPersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityPolicy, DbSecurityPolicy>
	{
        public SecurityPolicyPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<SecurityPolicy>[] orderBy)
        {
            if (orderBy == null || orderBy.Length == 0)
                orderBy = new ModelSort<SecurityPolicy>[]
                {
                    new ModelSort<SecurityPolicy>(o=>o.CreationTime, Core.Model.Map.SortOrderType.OrderByDescending)
                };
            return base.AppendOrderBy(rawQuery, orderBy);
        }

        /// <summary>
        /// Updating policies is a security risk and not permitted... ever
        /// </summary>
        public override Core.Model.Security.SecurityPolicy UpdateInternal(DataContext context, Core.Model.Security.SecurityPolicy data)
		{
            // Only public objects (created by the user) can be deleted
            if(!data.IsPublic)
			    throw new AdoFormalConstraintException(AdoFormalConstraintType.UpdatedReadonlyObject);
            return base.UpdateInternal(context, data);
		}
	}

	/// <summary>
	/// Security user persistence
	/// </summary>
	public class SecurityRolePersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityRole, DbSecurityRole>
	{

        public SecurityRolePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<SecurityRole>[] orderBy)
        {
            if (orderBy == null || orderBy.Length == 0)
                orderBy = new ModelSort<SecurityRole>[]
                {
                    new ModelSort<SecurityRole>(o=>o.UpdatedTime, Core.Model.Map.SortOrderType.OrderByDescending),
                    new ModelSort<SecurityRole>(o=>o.CreationTime, Core.Model.Map.SortOrderType.OrderByDescending)
                };
            return base.AppendOrderBy(rawQuery, orderBy);
        }

        /// <summary>
        /// Gets the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <param name="principal">The principal.</param>
        /// <returns>Returns a security role instance.</returns>
        internal override SecurityRole Get(DataContext context, Guid key)
		{
			var role = base.Get(context, key);

			if (role != null && context.LoadState == LoadState.FullLoad)
			{
				var policyQuery = context.CreateSqlStatement<DbSecurityRolePolicy>().SelectFrom(typeof(DbSecurityRolePolicy), typeof(DbSecurityPolicy))
										.InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
										.Where<DbSecurityRolePolicy>(o => o.SourceKey == role.Key);

                var mapper = this.m_settingsProvider.GetMapper();
				role.Policies = context.Query<CompositeResult<DbSecurityRolePolicy, DbSecurityPolicy>>(policyQuery).ToArray().Select(o => new SecurityPolicyInstance(mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
			}

			return role;
		}

		/// <summary>
		/// Insert the specified object
		/// </summary>
		public override Core.Model.Security.SecurityRole InsertInternal(DataContext context, Core.Model.Security.SecurityRole data)
		{
			var retVal = base.InsertInternal(context, data);

			if (data.Policies != null)
			{
                // TODO: Clean this up
                data.Policies.ForEach(o => o.PolicyKey = o.Policy?.EnsureExists(context)?.Key ?? o.PolicyKey);
                foreach (var pol in data.Policies.Select(o => new DbSecurityRolePolicy()

				{
					PolicyKey = o.PolicyKey.Value,
					GrantType = (int)o.GrantType,
					SourceKey = retVal.Key.Value,
					Key = Guid.NewGuid()
				}))
					context.Insert(pol);
			}

			return retVal;
		}

		/// <summary>
		/// Represent as model instance
		/// </summary>
		public override Core.Model.Security.SecurityRole ToModelInstance(object dataInstance, DataContext context)
		{
			var retVal = base.ToModelInstance(dataInstance, context);
			if (retVal == null) return null;

			var policyQuery = context.CreateSqlStatement<DbSecurityRolePolicy>().SelectFrom(typeof(DbSecurityRolePolicy), typeof(DbSecurityPolicy))
				.InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
				.Where<DbSecurityRolePolicy>(o => o.SourceKey == retVal.Key);
            var mapper = this.m_settingsProvider.GetMapper();
            retVal.Policies = context.Query<CompositeResult<DbSecurityRolePolicy, DbSecurityPolicy>>(policyQuery).ToArray().Select(o => new SecurityPolicyInstance(mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();

			var rolesQuery = context.CreateSqlStatement<DbSecurityUserRole>().SelectFrom(typeof(DbSecurityUser))
				.InnerJoin<DbSecurityUser>(o => o.UserKey, o => o.Key)
				.Where<DbSecurityUserRole>(o => o.RoleKey == retVal.Key);

            retVal.Users = context.Query<DbSecurityUser>(rolesQuery).ToArray().Select(o => mapper.MapDomainInstance<DbSecurityUser, Core.Model.Security.SecurityUser>(o)).ToList();

			return retVal;
		}

		/// <summary>
		/// Update the roles to security user
		/// </summary>
		public override Core.Model.Security.SecurityRole UpdateInternal(DataContext context, Core.Model.Security.SecurityRole data)
		 {
            var retVal = base.UpdateInternal(context, data);

            if (data.Policies != null)
			{
				context.Delete<DbSecurityRolePolicy>(o => o.SourceKey == data.Key);
                data.Policies.ForEach(o => o.PolicyKey = o.Policy?.EnsureExists(context)?.Key ?? o.PolicyKey);
                foreach (var pol in data.Policies.Select(o => new DbSecurityRolePolicy

				{
					PolicyKey = o.PolicyKey.Value,
					GrantType = (int)o.GrantType,
					SourceKey = data.Key.Value,
					Key = Guid.NewGuid()
				}))
					context.Insert(pol);
			}

			return data;
		}
	}

	/// <summary>
	/// Security user persistence
	/// </summary>
	public class SecurityUserPersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityUser, DbSecurityUser>
	{

        public SecurityUserPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        protected override SqlStatement AppendOrderBy(SqlStatement rawQuery, ModelSort<SecurityUser>[] orderBy)
        {
            if (orderBy == null || orderBy.Length == 0)
                orderBy = new ModelSort<SecurityUser>[]
                {
                    new ModelSort<SecurityUser>(o=>o.UpdatedTime, Core.Model.Map.SortOrderType.OrderByDescending),
                    new ModelSort<SecurityUser>(o=>o.CreationTime, Core.Model.Map.SortOrderType.OrderByDescending)
                };
            return base.AppendOrderBy(rawQuery, orderBy);
        }

        /// <summary>
        /// Insert the specified object
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        /// <param name="principal">The principal.</param>
        /// <returns>Core.Model.Security.SecurityUser.</returns>
        public override Core.Model.Security.SecurityUser InsertInternal(DataContext context, Core.Model.Security.SecurityUser data)
		{
			var retVal = base.InsertInternal(context, data);

			// Roles
			if (data.Roles != null)
				foreach (var r in data.Roles)
				{
					r.EnsureExists(context);
					context.Insert(new DbSecurityUserRole()
					{
						UserKey = retVal.Key.Value,
						RoleKey = r.Key.Value
					});
				}

            retVal.Password = null;
			return retVal;
		}

		/// <summary>
		/// Query internal
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="query">The query.</param>
		/// <param name="queryId">The query identifier.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The count.</param>
		/// <param name="totalResults">The total results.</param>
		/// <param name="principal">The principal.</param>
		/// <param name="countResults">if set to <c>true</c> [count results].</param>
		/// <returns>IEnumerable&lt;SecurityUser&gt;.</returns>
		public override IEnumerable<SecurityUser> QueryInternal(DataContext context, Expression<Func<SecurityUser, bool>> query, Guid queryId, int offset, int? count, out int totalResults, ModelSort<SecurityUser>[] orderBy, bool countResults = true)
		{
			var results = base.QueryInternal(context, query, queryId, offset, count, out totalResults, orderBy, countResults).ToList();

			var users = new List<SecurityUser>();
            var mapper = this.m_settingsProvider.GetMapper();
			foreach (var user in results)
			{
				var rolesQuery = context.CreateSqlStatement<DbSecurityUserRole>().SelectFrom(typeof(DbSecurityUserRole), typeof(DbSecurityRole))
					.InnerJoin<DbSecurityRole>(o => o.RoleKey, o => o.Key)
					.Where<DbSecurityUserRole>(o => o.UserKey == user.Key);

				user.Roles = context.Query<DbSecurityRole>(rolesQuery).ToArray().Select(o => mapper.MapDomainInstance<DbSecurityRole, SecurityRole>(o)).ToList();

				users.Add(user);
			}

			return users;
		}

		/// <summary>
		/// Maps the data to a model instance
		/// </summary>
		/// <param name="dataInstance">Data instance.</param>
		/// <param name="context">The context.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>The model instance.</returns>
		public override Core.Model.Security.SecurityUser ToModelInstance(object dataInstance, DataContext context)
		{
			var dbUser = dataInstance as DbSecurityUser;
			var retVal = base.ToModelInstance(dataInstance, context);
			if (retVal == null) return null;

			retVal.Lockout = dbUser?.Lockout;

			var rolesQuery = context.CreateSqlStatement<DbSecurityUserRole>().SelectFrom(typeof(DbSecurityRole))
				.InnerJoin<DbSecurityRole>(o => o.RoleKey, o => o.Key)
				.Where<DbSecurityUserRole>(o => o.UserKey == dbUser.Key);

            var mapper = this.m_settingsProvider.GetMapper();
			retVal.Roles = context.Query<DbSecurityRole>(rolesQuery).ToArray().Select(o => mapper.MapDomainInstance<DbSecurityRole, Core.Model.Security.SecurityRole>(o)).ToList();

			return retVal;
		}

		/// <summary>
		/// Update the roles to security user
		/// </summary>
		/// <param name="context">Context.</param>
		/// <param name="data">Data.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Core.Model.Security.SecurityUser.</returns>
		public override Core.Model.Security.SecurityUser UpdateInternal(DataContext context, Core.Model.Security.SecurityUser data)
		{
			var retVal = base.UpdateInternal(context, data);

			if (data.Roles == null || data.Roles.Count == 0)
			{
				return retVal;
			}

			context.Delete<DbSecurityUserRole>(o => o.UserKey == retVal.Key);
			foreach (var r in data.Roles)
			{
				r.EnsureExists(context);
				context.Insert(new DbSecurityUserRole()
				{
					UserKey = retVal.Key.Value,
					RoleKey = r.Key.Value
				});
			}

            retVal.Password = null;

            return retVal;
		}

		/// <summary>
		/// Gets a security user.
		/// </summary>
		/// <param name="context">The data context.</param>
		/// <param name="key">The key of the user to retrieve.</param>
		/// <param name="principal">The authentication context.</param>
		/// <returns>Returns a security user or null if no user is found.</returns>
		internal override SecurityUser Get(DataContext context, Guid key)
		{
			var user = base.Get(context, key);
			if (user == null) return null;
			var rolesQuery = context.CreateSqlStatement<DbSecurityUserRole>().SelectFrom(typeof(DbSecurityRole))
				.InnerJoin<DbSecurityRole>(o => o.RoleKey, o => o.Key)
				.Where<DbSecurityUserRole>(o => o.UserKey == key);

            var mapper = this.m_settingsProvider.GetMapper();
            user.Roles = context.Query<DbSecurityRole>(rolesQuery).ToArray().Select(o => mapper.MapDomainInstance<DbSecurityRole, SecurityRole>(o)).ToList();

			return user;
		}
	}
}