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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.PSQL.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;

using System.Security.Principal;
using SanteDB.Server.Core.Security;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a PIP fed from SQL Server tables
    /// </summary>
    [ServiceProvider("ADO.NET Policy Information Service")]
    public class AdoPolicyInformationService : IPolicyInformationService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "ADO.NET Policy Information Service";

        // Get the SQL configuration
        private AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        // Ad-hoc cache
        private IAdhocCacheService m_adhocCache;

        private Tracer m_traceSource = new Tracer(AdoDataConstants.IdentityTraceSourceName);

        /// <summary>
        /// Create new service with adhoc cache
        /// </summary>
        public AdoPolicyInformationService(IAdhocCacheService adhocCache = null)
        {
            this.m_adhocCache = adhocCache;
        }


        /// <summary>
        /// Adds the specified policies to the specified object
        /// </summary>
        /// <param name="securable">The securible to which the policy is to be added</param>
        /// <param name="rule">The rule to apply to the securable</param>
        /// <param name="policyOids">The policy OIDs to apply</param>
        public void AddPolicies(object securable, PolicyGrantType rule, IPrincipal principal, params string[] policyOids)
        {

            using (DataContext context = this.m_configuration.Provider.GetWriteConnection())
            {
                IDbTransaction tx = null;
                try
                {
                    context.Open();
                    tx = context.BeginTransaction();
                    foreach (var oid in policyOids)
                    {
                        var policy = context.FirstOrDefault<DbSecurityPolicy>(p => p.Oid == oid);
                        if (policy == null) throw new KeyNotFoundException($"Policy {oid} not found");

                        // Add
                        if (securable is SecurityRole sr)
                        {
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AssignPolicy, principal).Demand();
                            // Delete the existing role
                            context.Delete<DbSecurityRolePolicy>(o => o.PolicyKey == policy.Key && o.SourceKey == sr.Key);
                            context.Insert(new DbSecurityRolePolicy()
                            {
                                SourceKey = sr.Key.Value,
                                GrantType = (int)rule,
                                PolicyKey = policy.Key
                            });
                        }
                        else if (securable is SecurityApplication sa)
                        {
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AssignPolicy, principal).Demand();
                            // Delete the existing role
                            context.Delete<DbSecurityApplicationPolicy>(o => o.PolicyKey == policy.Key && o.SourceKey == sa.Key);
                            context.Insert(new DbSecurityApplicationPolicy()
                            {
                                SourceKey = sa.Key.Value,
                                GrantType = (int)rule,
                                PolicyKey = policy.Key
                            });

                        }
                        else if (securable is SecurityDevice sd)
                        {
                            // Delete the existing role
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AssignPolicy, principal).Demand();
                            context.Delete<DbSecurityDevicePolicy>(o => o.PolicyKey == policy.Key && o.SourceKey == sd.Key);
                            context.Insert(new DbSecurityDevicePolicy()
                            {
                                SourceKey = sd.Key.Value,
                                GrantType = (int)rule,
                                PolicyKey = policy.Key
                            });
                        }
                        else if (securable is Entity ent)
                        {
                            // Must either have write, must be the patient or must be their dedicated provider
                            if (securable is Patient)
                                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.WriteClinicalData, principal).Demand();
                            else if (securable is Material || securable is ManufacturedMaterial)
                                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.WriteMaterials, principal).Demand();
                            else if (securable is Place || securable is Organization || securable is Provider)
                                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.WritePlacesAndOrgs, principal).Demand();
                            else
                                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AssignPolicy, principal).Demand();

                            var existing = context.FirstOrDefault<DbEntitySecurityPolicy>(e => e.SourceKey == ent.Key && e.PolicyKey == policy.Key && e.ObsoleteVersionSequenceId == null);
                            if (existing != null)
                            {
                                // Set obsolete to null if rule is DENY
                                existing.ObsoleteVersionSequenceId = null;
                                context.Update(existing);
                            }
                            context.Insert(new DbEntitySecurityPolicy()
                            {
                                EffectiveVersionSequenceId = ent.VersionSequence.Value,
                                PolicyKey = policy.Key,
                                SourceKey = ent.Key.Value
                            });

                        }
                        else if (securable is Act act)
                        {

                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.WriteClinicalData, principal).Demand();
                            var existing = context.FirstOrDefault<DbActSecurityPolicy>(e => e.SourceKey == act.Key && e.PolicyKey == policy.Key && e.ObsoleteVersionSequenceId == null);

                            if (existing != null)
                            {
                                // Set obsolete to null if rule is DENY
                                existing.ObsoleteVersionSequenceId = null;
                                context.Update(existing);
                            }
                            context.Insert(new DbActSecurityPolicy()
                            {
                                EffectiveVersionSequenceId = act.VersionSequence.Value,
                                PolicyKey = policy.Key,
                                SourceKey = act.Key.Value
                            });

                        }
                        else
                            throw new NotSupportedException($"Policies are not supported for {securable}");
                    }
                    tx.Commit();

                    if (securable is IdentifiedData id)
                        this.m_adhocCache?.Remove($"pip.{id.GetType().Name}.{id.Key}.{id.Tag}");

                }
                catch (Exception e)
                {
                    tx.Rollback();
                    this.m_traceSource.TraceEvent(EventLevel.Error, "Error adding policies to {0}: {1}", securable, e);
                    throw;
                }
            }
        }

        /// <summary>
        /// REmoves the specified policies to the specified object
        /// </summary>
        /// <param name="securable">The securible to which the policy is to be removed</param>
        /// <param name="rule">The rule to apply to the securable</param>
        /// <param name="policyOids">The policy OIDs to remove</param>
        public void RemovePolicies(object securable, IPrincipal principal, params string[] policyOids)
        {

            using (DataContext context = this.m_configuration.Provider.GetWriteConnection())
            {
                IDbTransaction tx = null;
                try
                {
                    context.Open();
                    tx = context.BeginTransaction();
                    foreach (var oid in policyOids)
                    {
                        var policy = context.FirstOrDefault<DbSecurityPolicy>(p => p.Oid == oid);
                        if (policy == null) throw new KeyNotFoundException($"Policy {oid} not found");

                        // Add
                        if (securable is SecurityRole sr)
                        {
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AssignPolicy, principal).Demand();
                            // Delete the existing role
                            context.Delete<DbSecurityRolePolicy>(o => o.PolicyKey == policy.Key && o.SourceKey == sr.Key);
                        }
                        else if (securable is SecurityApplication sa)
                        {
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AssignPolicy, principal).Demand();
                            // Delete the existing role
                            context.Delete<DbSecurityApplicationPolicy>(o => o.PolicyKey == policy.Key && o.SourceKey == sa.Key);
                        }
                        else if (securable is SecurityDevice sd)
                        {
                            // Delete the existing role
                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AssignPolicy, principal).Demand();
                            context.Delete<DbSecurityDevicePolicy>(o => o.PolicyKey == policy.Key && o.SourceKey == sd.Key);
                        }
                        else if (securable is Entity ent)
                        {
                            // Must either have write, must be the patient or must be their dedicated provider
                            if (securable is Patient)
                                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.WriteClinicalData, principal).Demand();
                            else if (securable is Material || securable is ManufacturedMaterial)
                                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.WriteMaterials, principal).Demand();
                            else if (securable is Place || securable is Organization || securable is Provider)
                                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.WritePlacesAndOrgs, principal).Demand();
                            else
                                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.AssignPolicy, principal).Demand();

                            var existing = context.FirstOrDefault<DbEntitySecurityPolicy>(e => e.SourceKey == ent.Key && e.PolicyKey == policy.Key && e.ObsoleteVersionSequenceId == null);
                            if (existing != null)
                            {
                                // Set obsolete to null if rule is DENY
                                existing.ObsoleteVersionSequenceId = null;
                                context.Update(existing);
                            }
                        }
                        else if (securable is Act act)
                        {

                            new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.WriteClinicalData, principal).Demand();
                            var existing = context.FirstOrDefault<DbActSecurityPolicy>(e => e.SourceKey == act.Key && e.PolicyKey == policy.Key && e.ObsoleteVersionSequenceId == null);

                            if (existing != null)
                            {
                                // Set obsolete to null if rule is DENY
                                existing.ObsoleteVersionSequenceId = null;
                                context.Update(existing);
                            }

                        }
                        else
                            throw new NotSupportedException($"Policies are not supported for {securable}");
                    }
                    tx.Commit();

                    if (securable is IdentifiedData id)
                        this.m_adhocCache?.Remove($"pip.{id.GetType().Name}.{id.Key}.{id.Tag}");
                }
                catch (Exception e)
                {
                    tx.Rollback();
                    this.m_traceSource.TraceEvent(EventLevel.Error, "Error adding policies to {0}: {1}", securable, e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Get active policies for the specified securable type
        /// </summary>
        public IEnumerable<IPolicyInstance> GetPolicies(object securable)
        {
    
            List<AdoSecurityPolicyInstance> result = null;
            if (securable is IdentifiedData id)
                result = this.m_adhocCache?.Get<List<AdoSecurityPolicyInstance>>($"pip.{id.GetType().Name}.{id.Key}.{id.Tag}");
            else
                id = null;

            if (result == null)
            {
                using (DataContext context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    try
                    {
                        context.Open();


                        // Security device
                        if (securable is Core.Model.Security.SecurityDevice sd)
                        {
                            var query = context.CreateSqlStatement<DbSecurityDevicePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityDevicePolicy))
                                .AutoJoin<DbSecurityPolicy, DbSecurityDevicePolicy>();

                            if (securable is DevicePrincipal dp)
                            {

                                query.AutoJoin<DbSecurityDevice, DbSecurityDevice>()
                                        .Where(o => o.PublicId == dp.Identity.Name);

                                var retVal = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityDevicePolicy>>(query)
                                    .AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();

                                var appClaim = dp.Identities.OfType<Server.Core.Security.ApplicationIdentity>().SingleOrDefault()?.FindAll(SanteDBClaimTypes.Sid).SingleOrDefault() ??
                                       dp.FindAll(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim).SingleOrDefault();

                                // There is an application claim so we want to add the application policies - most restrictive
                                if (appClaim != null)
                                {
                                    var claim = Guid.Parse(appClaim.Value);

                                    var aquery = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityApplicationPolicy))
                                       .AutoJoin<DbSecurityPolicy, DbSecurityApplicationPolicy>()
                                       .Where(o => o.SourceKey == claim);

                                    retVal.AddRange(context.Query<CompositeResult<DbSecurityPolicy, DbSecurityApplicationPolicy>>(aquery).AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)));
                                }

                                result = retVal;
                            }
                            else
                            {
                                result = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityDevicePolicy>>(query.Where(o => o.SourceKey == sd.Key))
                                    .AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();
                            }
                        }
                        else if (securable is Core.Model.Security.SecurityRole sr)
                        {
                            var query = context.CreateSqlStatement<DbSecurityRolePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityRolePolicy))
                                .AutoJoin<DbSecurityPolicy, DbSecurityRolePolicy>()
                                .Where(o => o.SourceKey == sr.Key);

                            result = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityRolePolicy>>(query)
                                .AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();
                        }
                        else if (securable is Core.Model.Security.SecurityApplication sa)
                        {
                            var query = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityApplicationPolicy))
                                .AutoJoin<DbSecurityPolicy, DbSecurityApplicationPolicy>();

                            if (securable is ApplicationPrincipal ap)
                                query.AutoJoin<DbSecurityApplication, DbSecurityApplication>()
                                        .Where(o => o.PublicId == ap.Identity.Name);
                            else
                                query.Where(o => o.SourceKey == sa.Key);

                            result = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityApplicationPolicy>>(query)
                                .AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();
                        }
                        else if (securable is IPrincipal || securable is IIdentity)
                        {
                            var identity = (securable as IPrincipal)?.Identity ?? securable as IIdentity;

                            IEnumerable<CompositeResult<DbSecurityPolicy, DbSecurityPolicyActionableInstance>> retVal = null;

                            SqlStatement query = null;
                            if (!(identity is Server.Core.Security.ApplicationIdentity) &&
                                !(identity is DeviceIdentity)) // Is this a user based claim?
                            {
                                // Role policies
                                query = context.CreateSqlStatement<DbSecurityRolePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityPolicyActionableInstance))
                                    .InnerJoin<DbSecurityRolePolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                                    .InnerJoin<DbSecurityRolePolicy, DbSecurityUserRole>(o => o.SourceKey, o => o.RoleKey)
                                    .InnerJoin<DbSecurityUserRole, DbSecurityUser>(o=>o.UserKey, o => o.Key)
                                    .Where<DbSecurityUser>(o => o.UserName.ToLower() == identity.Name.ToLower());

                                retVal = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityPolicyActionableInstance>>(query).AsEnumerable();
                            }

                            // Claims principal, then we want device and app SID
                            if (securable is IClaimsPrincipal cp)
                            {
                                var appClaim = cp.Identities.OfType<Server.Core.Security.ApplicationIdentity>().SingleOrDefault()?.FindAll(SanteDBClaimTypes.Sid).SingleOrDefault() ??
                                    cp.FindAll(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim).SingleOrDefault();
                                var devClaim = cp.Identities.OfType<Server.Core.Security.DeviceIdentity>().SingleOrDefault()?.FindAll(SanteDBClaimTypes.Sid).SingleOrDefault() ??
                                    cp.FindAll(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim).SingleOrDefault();


                                // There is an application claim so we want to add the application policies - most restrictive
                                if (appClaim != null)
                                {
                                    var claim = Guid.Parse(appClaim.Value);
                                    query = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityPolicyActionableInstance))
                                       .AutoJoin<DbSecurityPolicy, DbSecurityApplicationPolicy>()
                                       .Where(o => o.SourceKey == claim);

                                    if(retVal != null)
                                    {
                                        var usrPolKeys = retVal.AsEnumerable().Select(o => o.Object2.PolicyKey).ToArray(); // App grant only overrides those policies which already exist on user
                                        query.And<DbSecurityApplicationPolicy>(o => usrPolKeys.Contains(o.PolicyKey));
                                    }

                                    var appResults = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityPolicyActionableInstance>>(query);
                                    retVal = retVal?.Union(appResults) ?? appResults;
                                }

                                // There is an device claim so we want to add the device policies - most restrictive
                                if (devClaim != null)
                                {
                                    var claim = Guid.Parse(devClaim.Value);
                                    query = context.CreateSqlStatement<DbSecurityDevicePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityPolicyActionableInstance))
                                       .AutoJoin<DbSecurityPolicy, DbSecurityDevicePolicy>()
                                       .Where(o => o.SourceKey == claim);

                                    if (retVal != null)
                                    {
                                        var usrPolKeys = retVal.Select(o => o.Object2.PolicyKey).ToArray(); // App grant only overrides those policies which already exist on user
                                        query.And<DbSecurityDevicePolicy>(o => usrPolKeys.Contains(o.PolicyKey));
                                    }

                                    var devResults = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityPolicyActionableInstance>>(query);
                                    retVal = retVal?.Union(devResults) ?? devResults;
                                }

                            }

                            result = retVal.AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();
                            this.m_traceSource.TraceEvent(EventLevel.Verbose, "Principal {0} effective policy set {1}", identity?.Name, String.Join(",", result.Select(o => $"{o.Policy.Oid} [{o.Rule}]")));
                        }
                        else if (securable is Core.Model.Acts.Act pAct)
                        {
                            var query = context.CreateSqlStatement<DbActSecurityPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbActSecurityPolicy))
                                      .AutoJoin<DbSecurityPolicy, DbActSecurityPolicy>()
                                      .Where(o => o.SourceKey == pAct.Key);

                            result = context.Query<CompositeResult<DbSecurityPolicy, DbActSecurityPolicy>>(query)
                                .AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();
                        }
                        else if (securable is Core.Model.Entities.Entity pEntity)
                        {
                            var query = context.CreateSqlStatement<DbEntitySecurityPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbEntitySecurityPolicy))
                                      .AutoJoin<DbSecurityPolicy, DbEntitySecurityPolicy>()
                                      .Where(o => o.SourceKey == pEntity.Key);

                            result = context.Query<CompositeResult<DbSecurityPolicy, DbEntitySecurityPolicy>>(query)
                                .AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();
                        }
                        else if (securable is SecurityUser pUser)
                        {
                            // Join for policies
                            var query = context.CreateSqlStatement<DbSecurityUserRole>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityRolePolicy))
                                .InnerJoin<DbSecurityUserRole, DbSecurityRolePolicy>(
                                    o => o.RoleKey,
                                    o => o.SourceKey
                                )
                                .InnerJoin<DbSecurityRolePolicy, DbSecurityPolicy>(
                                    o => o.PolicyKey,
                                    o => o.Key
                                )
                                .Where<DbSecurityUserRole>(o => o.UserKey == pUser.Key);

                            result = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityRolePolicy>>(query)
                                .AsEnumerable().Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();

                        }
                        else
                            result = new List<AdoSecurityPolicyInstance>();

                        // Cache
                        if (id != null)
                            this.m_adhocCache?.Add($"pip.{id.GetType().Name}.{id.Key}.{id.Tag}", result);
                            

                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceEvent(EventLevel.Error, "Error getting active policies for {0} : {1}", securable, e);
                        throw new Exception($"Error getting active policies for {securable}", e);
                    }
                }
            }
            return result;
        }

        
        /// <summary>
        /// Get all policies on the system
        /// </summary>
        public IEnumerable<IPolicy> GetPolicies()
        {
            using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    dataContext.Open();
                    return dataContext.Query<DbSecurityPolicy>(o => o.ObsoletionTime == null).ToArray().Select(o => new AdoSecurityPolicy(o)).ToArray();
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(EventLevel.Error, "Error getting policies: {0}", e);
                    throw;
                }
        }

        /// <summary>
        /// Get a specific policy
        /// </summary>
        public IPolicy GetPolicy(string policyOid)
        {
            var retVal = this.m_adhocCache?.Get<DbSecurityPolicy>($"pip.{policyOid}");

            if (retVal == null)
            {
                using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    try
                    {
                        dataContext.Open();
                        var policy = dataContext.SingleOrDefault<DbSecurityPolicy>(o => o.Oid == policyOid);
                        if (policy != null)
                        {
                            this.m_adhocCache.Add($"pip.{policyOid}", policy);
                            return new AdoSecurityPolicy(policy);
                        }
                        return null;
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceEvent(EventLevel.Error, "Error getting policy {0} : {1}", policyOid, e);
                        throw;
                    }
                }
            }
            else 
                return new AdoSecurityPolicy(retVal);

        }

        /// <summary>
        /// Gets the specified policy instance (if applicable) for the specified object
        /// </summary>
        public IPolicyInstance GetPolicyInstance(object securable, string policyOid)
        {
            // TODO: Add caching for this
            return this.GetPolicies(securable).FirstOrDefault(o => o.Policy.Oid == policyOid);
        }
    }
}
