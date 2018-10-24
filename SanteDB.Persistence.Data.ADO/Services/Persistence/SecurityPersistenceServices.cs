/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2017-9-1
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.Security;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.ADO.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using MARC.HI.EHRS.SVC.Core.Data;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{

    /// <summary>
    /// Security provenance service
    /// </summary>
    public class SecurityProvenancePersistenceService : IdentifiedPersistenceService<SecurityProvenance, DbSecurityProvenance>
    {
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

        /// <summary>
        /// Querying for provenance is not supported
        /// </summary>
        protected override IEnumerable<object> DoQueryInternal(DataContext context, Expression<Func<SecurityProvenance, bool>> query, Guid queryId, int offset, int? count, out int totalResults, bool incudeCount = true)
        {
            throw new NotSupportedException();
        }
    }
	/// <summary>
	/// Security user persistence
	/// </summary>
	public class SecurityApplicationPersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityApplication, DbSecurityApplication>
	{
		internal override SecurityApplication Get(DataContext context, Guid key)
		{
			var application = base.Get(context, key);

			if (application != null && context.LoadState == LoadState.FullLoad)
			{
				var policyQuery = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom()
										.InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
										.Where<DbSecurityApplicationPolicy>(o => o.SourceKey == application.Key);

				application.Policies = context.Query<CompositeResult<DbSecurityApplicationPolicy, DbSecurityPolicy>>(policyQuery).Select(o => new SecurityPolicyInstance(m_mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
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

			data.Policies.ForEach(o => o.Policy?.EnsureExists(context));
			foreach (var itm in data.Policies.Select(o => new DbSecurityApplicationPolicy()
			{
				PolicyKey = o.PolicyKey.Value,
				GrantType = (int)o.GrantType,
				SourceKey = retVal.Key.Value,
				Key = Guid.NewGuid()
			}))
				context.Insert(itm);

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

			retVal.Policies = context.Query<CompositeResult<DbSecurityApplicationPolicy, DbSecurityPolicy>>(policyQuery).Select(o => new SecurityPolicyInstance(m_mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
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
			var domainInstance = this.FromModelInstance(data, context);

			var currentObject = context.FirstOrDefault<DbSecurityApplication>(d => d.Key == data.Key);

			if (currentObject == null)
			{
				throw new KeyNotFoundException(data.Key.ToString());
			}

			currentObject.CopyObjectData(domainInstance);

			currentObject.ObsoletedByKey = data.ObsoletedByKey == Guid.Empty ? null : data.ObsoletedByKey;
			currentObject.ObsoletionTime = data.ObsoletionTime;

			context.Update(currentObject);

			if (data.Policies != null)
			{
				context.Delete<DbSecurityApplicationPolicy>(o => o.SourceKey == data.Key);
				foreach (var pol in data.Policies.Select(o => new DbSecurityApplicationPolicy
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
	public class SecurityDevicePersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityDevice, DbSecurityDevice>
	{
		internal override SecurityDevice Get(DataContext context, Guid key)
		{
			var device = base.Get(context, key);

			if (device != null && context.LoadState == LoadState.FullLoad)
			{
				var policyQuery = context.CreateSqlStatement<DbSecurityDevicePolicy>().SelectFrom()
										.InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
										.Where<DbSecurityDevicePolicy>(o => o.SourceKey == device.Key);

				device.Policies = context.Query<CompositeResult<DbSecurityDevicePolicy, DbSecurityPolicy>>(policyQuery).Select(o => new SecurityPolicyInstance(m_mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
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

			data.Policies.ForEach(o => o.Policy?.EnsureExists(context));
			foreach (var itm in data.Policies.Select(o => new DbSecurityDevicePolicy()
			{
				PolicyKey = o.PolicyKey.Value,
				GrantType = (int)o.GrantType,
				SourceKey = retVal.Key.Value,
				Key = Guid.NewGuid()
			}))
				context.Insert(itm);

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

			retVal.Policies = context.Query<CompositeResult<DbSecurityDevicePolicy, DbSecurityPolicy>>(policyQuery).Select(o => new SecurityPolicyInstance(m_mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
			return retVal;
		}

		/// <summary>
		/// Update the roles to security user
		/// </summary>
		public override Core.Model.Security.SecurityDevice UpdateInternal(DataContext context, Core.Model.Security.SecurityDevice data)
		{
			var domainInstance = this.FromModelInstance(data, context);

			var currentObject = context.FirstOrDefault<DbSecurityDevice>(d => d.Key == data.Key);

			if (currentObject == null)
			{
				throw new KeyNotFoundException(data.Key.ToString());
			}

			currentObject.CopyObjectData(domainInstance);

			currentObject.ObsoletedByKey = data.ObsoletedByKey == Guid.Empty ? null : data.ObsoletedByKey;
			currentObject.ObsoletionTime = data.ObsoletionTime;

			context.Update(currentObject);

			if (data.Policies != null)
			{
				context.Delete<DbSecurityDevicePolicy>(o => o.SourceKey == data.Key);
				foreach (var pol in data.Policies.Select(o => new DbSecurityDevicePolicy
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
	/// Represents a persistence service for security policies.
	/// </summary>
	public class SecurityPolicyPersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityPolicy, DbSecurityPolicy>
	{
		/// <summary>
		/// Updating policies is a security risk and not permitted... ever
		/// </summary>
		public override Core.Model.Security.SecurityPolicy UpdateInternal(DataContext context, Core.Model.Security.SecurityPolicy data)
		{
			throw new AdoFormalConstraintException(AdoFormalConstraintType.UpdatedReadonlyObject);
		}
	}

	/// <summary>
	/// Security user persistence
	/// </summary>
	public class SecurityRolePersistenceService : BaseDataPersistenceService<Core.Model.Security.SecurityRole, DbSecurityRole>
	{
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

				role.Policies = context.Query<CompositeResult<DbSecurityRolePolicy, DbSecurityPolicy>>(policyQuery).Select(o => new SecurityPolicyInstance(m_mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();
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
				data.Policies.ForEach(o => o.Policy?.EnsureExists(context));
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

			retVal.Policies = context.Query<CompositeResult<DbSecurityRolePolicy, DbSecurityPolicy>>(policyQuery).Select(o => new SecurityPolicyInstance(m_mapper.MapDomainInstance<DbSecurityPolicy, SecurityPolicy>(o.Object2), (PolicyGrantType)o.Object1.GrantType)).ToList();

			var rolesQuery = context.CreateSqlStatement<DbSecurityUserRole>().SelectFrom(typeof(DbSecurityUser))
				.InnerJoin<DbSecurityUser>(o => o.UserKey, o => o.Key)
				.Where<DbSecurityUserRole>(o => o.RoleKey == retVal.Key);

			retVal.Users = context.Query<DbSecurityUser>(rolesQuery).Select(o => m_mapper.MapDomainInstance<DbSecurityUser, Core.Model.Security.SecurityUser>(o)).ToList();

			return retVal;
		}

		/// <summary>
		/// Update the roles to security user
		/// </summary>
		public override Core.Model.Security.SecurityRole UpdateInternal(DataContext context, Core.Model.Security.SecurityRole data)
		{
			var domainInstance = this.FromModelInstance(data, context);

			var currentObject = context.FirstOrDefault<DbSecurityRole>(d => d.Key == data.Key);

			if (currentObject == null)
			{
				throw new KeyNotFoundException(data.Key.ToString());
			}

			currentObject.CopyObjectData(domainInstance);

			currentObject.ObsoletedByKey = data.ObsoletedByKey == Guid.Empty ? null : data.ObsoletedByKey;
			currentObject.ObsoletionTime = data.ObsoletionTime;

			context.Update(currentObject);

			if (data.Policies != null)
			{
				context.Delete<DbSecurityRolePolicy>(o => o.SourceKey == data.Key);
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
		public override IEnumerable<SecurityUser> QueryInternal(DataContext context, Expression<Func<SecurityUser, bool>> query, Guid queryId, int offset, int? count, out int totalResults, bool countResults = true)
		{
			var results = base.QueryInternal(context, query, queryId, offset, count, out totalResults, countResults);

			var users = new List<SecurityUser>();

			foreach (var user in results)
			{
				var rolesQuery = context.CreateSqlStatement<DbSecurityUserRole>().SelectFrom(typeof(DbSecurityUserRole), typeof(DbSecurityRole))
					.InnerJoin<DbSecurityRole>(o => o.RoleKey, o => o.Key)
					.Where<DbSecurityUserRole>(o => o.UserKey == user.Key);

				user.Roles = context.Query<DbSecurityRole>(rolesQuery).Select(o => m_mapper.MapDomainInstance<DbSecurityRole, SecurityRole>(o)).ToList();

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

			retVal.Roles = context.Query<DbSecurityRole>(rolesQuery).Select(o => m_mapper.MapDomainInstance<DbSecurityRole, Core.Model.Security.SecurityRole>(o)).ToList();
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

			if (data.Roles == null)
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

			user.Roles = context.Query<DbSecurityRole>(rolesQuery).Select(o => m_mapper.MapDomainInstance<DbSecurityRole, SecurityRole>(o)).ToList();

			return user;
		}
	}
}