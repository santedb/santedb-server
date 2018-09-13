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
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Services.Policy;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.PSQL.Security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a PIP fed from SQL Server tables
    /// </summary>
    public class AdoPolicyInformationService : IPolicyInformationService
    {
        // Get the SQL configuration
        private AdoConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(AdoDataConstants.ConfigurationSectionName) as AdoConfiguration;

        private TraceSource m_traceSource = new TraceSource(AdoDataConstants.IdentityTraceSourceName);

        /// <summary>
        /// Adds the specified policies to the specified object
        /// </summary>
        /// <param name="securable">The securible to which the policy is to be added</param>
        /// <param name="rule">The rule to apply to the securable</param>
        /// <param name="policyOids">The policy OIDs to apply</param>
        public void AddPolicies(object securable, PolicyDecisionOutcomeType rule, params string[] policyOids)
        {
            using (DataContext context = this.m_configuration.Provider.GetWriteConnection())
            {
                IDbTransaction tx = null;
                try
                {
                    context.Open();
                    tx = context.BeginTransaction();
                    foreach (var oid in policyOids) {
                        var policy = context.FirstOrDefault<DbSecurityPolicy>(p => p.Oid == oid);
                        if (policy == null) throw new KeyNotFoundException($"Policy {oid} not found");

                        // Add
                        if (securable is SecurityRole)
                        {
                            // Delete the existing role
                            context.Delete<DbSecurityRolePolicy>(o => o.PolicyKey == policy.Key);
                            context.Insert(new DbSecurityRolePolicy()
                            {
                                SourceKey = (securable as SecurityRole).Key.Value,
                                GrantType = (int)rule,
                                PolicyKey = policy.Key
                            });
                        }
                        else if (securable is SecurityApplication)
                        {
                            // Delete the existing role
                            context.Delete<DbSecurityApplicationPolicy>(o => o.PolicyKey == policy.Key);
                            context.Insert(new DbSecurityApplicationPolicy()
                            {
                                SourceKey = (securable as SecurityApplication).Key.Value,
                                GrantType = (int)rule,
                                PolicyKey = policy.Key
                            });

                        }
                        else if (securable is SecurityDevice)
                        {
                            // Delete the existing role
                            context.Delete<DbSecurityDevicePolicy>(o => o.PolicyKey == policy.Key);
                            context.Insert(new DbSecurityDevicePolicy()
                            {
                                SourceKey = (securable as SecurityDevice).Key.Value,
                                GrantType = (int)rule,
                                PolicyKey = policy.Key
                            });
                        }
                        else if (securable is Entity)
                        {
                            var ent = securable as Entity;
                            var existing = context.FirstOrDefault<DbEntitySecurityPolicy>(e => e.SourceKey == ent.Key && e.PolicyKey == policy.Key);
                            if (existing != null)
                                context.Insert(new DbEntitySecurityPolicy()
                                {
                                    EffectiveVersionSequenceId = ent.VersionSequence.Value,
                                    PolicyKey = policy.Key,
                                    SourceKey = ent.Key.Value
                                });
                            else
                            {
                                // Set obsolete to null if rule is DENY
                                existing.ObsoleteVersionSequenceId = null;
                                context.Update(existing);
                            }
                        }
                        else if (securable is Act)
                        {
                            var act = securable as Act;
                            var existing = context.FirstOrDefault<DbActSecurityPolicy>(e => e.SourceKey == act.Key && e.PolicyKey == policy.Key);
                            if (existing != null)
                                context.Insert(new DbActSecurityPolicy()
                                {
                                    EffectiveVersionSequenceId = act.VersionSequence.Value,
                                    PolicyKey = policy.Key,
                                    SourceKey = act.Key.Value
                                });
                            else
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
                }
                catch (Exception e)
                {
                    tx.Rollback();
                    this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error adding policies to {0}: {1}", securable, e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Get active policies for the specified securable type
        /// </summary>
        public IEnumerable<IPolicyInstance> GetActivePolicies(object securable)
        {
            using (DataContext context = this.m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    context.Open();
                    // Security device
                    if (securable is Core.Model.Security.SecurityDevice || securable is DevicePrincipal)
                    {
                        var query = context.CreateSqlStatement<DbSecurityDevicePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityDevicePolicy))
                            .InnerJoin<DbSecurityPolicy, DbSecurityDevicePolicy>();

                        if (securable is DevicePrincipal)
                            query.InnerJoin<DbSecurityDevice, DbSecurityDevice>()
                                    .Where(o => o.PublicId == (securable as DevicePrincipal).Identity.Name);
                        else
                            query.Where(o => o.Key == (securable as IdentifiedData).Key);

                        var retVal = context.Query<CompositeResult<DbSecurityPolicy, DbSecurityDevicePolicy>>(query)
                                                    .Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).ToList();

                        // App claim
                        if (securable is DevicePrincipal)
                        {
                            var cp = securable as DevicePrincipal;

                            var appClaim = cp.Identities.OfType<Core.Security.ApplicationIdentity>().SingleOrDefault()?.FindAll(ClaimTypes.Sid).SingleOrDefault() ??
                                   cp.FindAll(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim).SingleOrDefault();
                            
                            // There is an application claim so we want to add the application policies - most restrictive
                            if (appClaim != null)
                            {
                                var claim = Guid.Parse(appClaim.Value);

                                var aquery = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityApplicationPolicy))
                                   .InnerJoin<DbSecurityPolicy, DbSecurityApplicationPolicy>()
                                   .Where(o => o.SourceKey == claim);

                                retVal.AddRange(context.Query<CompositeResult<DbSecurityPolicy, DbSecurityApplicationPolicy>>(aquery).Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable)).Where(p => retVal.Any(r => r.Policy.Oid == p.Policy.Oid)));
                            }
                        }

                        return retVal;
					}
                    else if (securable is Core.Model.Security.SecurityRole)
                    {
                        var query = context.CreateSqlStatement<DbSecurityRolePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityRolePolicy))
                            .InnerJoin<DbSecurityPolicy, DbSecurityRolePolicy>()
                            .Where(o => o.SourceKey == (securable as IdentifiedData).Key);

                        return context.Query<CompositeResult<DbSecurityPolicy, DbSecurityRolePolicy>>(query)
                            .Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable));
                    }
                    else if (securable is Core.Model.Security.SecurityApplication || securable is ApplicationPrincipal)
                    {
                        var query = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityApplicationPolicy))
                            .InnerJoin<DbSecurityPolicy, DbSecurityApplicationPolicy>();

                        if (securable is ApplicationPrincipal)
                            query.InnerJoin<DbSecurityApplication, DbSecurityApplication>()
                                    .Where(o => o.PublicId == (securable as ApplicationPrincipal).Identity.Name);
                        else
                            query.Where(o => o.Key == (securable as IdentifiedData).Key);

                        return context.Query<CompositeResult<DbSecurityPolicy, DbSecurityApplicationPolicy>>(query)
                            .Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable));
                    }
                    else if (securable is IPrincipal || securable is IIdentity)
                    {
                        var identity = (securable as IPrincipal)?.Identity ?? securable as IIdentity;
                        var user = context.SingleOrDefault<DbSecurityUser>(u => u.UserName.ToLower() == identity.Name.ToLower());
                        if (user == null)
                            throw new KeyNotFoundException("Identity not found");

                        List<IPolicyInstance> retVal = new List<IPolicyInstance>();

                        // Role policies
                        SqlStatement query = context.CreateSqlStatement<DbSecurityRolePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityRolePolicy))
                            .InnerJoin<DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                            .InnerJoin<DbSecurityUserRole>(o => o.SourceKey, o => o.RoleKey)
                            .Where<DbSecurityUserRole>(o => o.UserKey == user.Key);

                        retVal.AddRange(context.Query<CompositeResult<DbSecurityPolicy, DbSecurityRolePolicy>>(query).Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, user)));

                        // Claims principal, then we want device and app SID
                        if (securable is ClaimsPrincipal)
                        {
                            var cp = securable as ClaimsPrincipal;
                            var appClaim = cp.Identities.OfType<Core.Security.ApplicationIdentity>().SingleOrDefault()?.FindAll(ClaimTypes.Sid).SingleOrDefault() ??
                                cp.FindAll(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim).SingleOrDefault();
                            var devClaim = cp.Identities.OfType<Core.Security.DeviceIdentity>().SingleOrDefault()?.FindAll(ClaimTypes.Sid).SingleOrDefault() ?? 
                                cp.FindAll(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim).SingleOrDefault();


                            // There is an application claim so we want to add the application policies - most restrictive
                            if (appClaim != null)
                            {
                                var claim = Guid.Parse(appClaim.Value);

                                query = context.CreateSqlStatement<DbSecurityApplicationPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityApplicationPolicy))
                                   .InnerJoin<DbSecurityPolicy, DbSecurityApplicationPolicy>()
                                   .Where(o => o.SourceKey == claim);

                                retVal.AddRange(context.Query<CompositeResult<DbSecurityPolicy, DbSecurityApplicationPolicy>>(query).Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, user)).Where(p=>retVal.Any(r=>r.Policy.Oid == p.Policy.Oid)));
                            }

                            // There is an device claim so we want to add the device policies - most restrictive
                            if (devClaim != null)
                            {
                                var claim = Guid.Parse(devClaim.Value);

                                query = context.CreateSqlStatement<DbSecurityDevicePolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbSecurityDevicePolicy))
                                   .InnerJoin<DbSecurityPolicy, DbSecurityDevicePolicy>()
                                   .Where(o => o.SourceKey == claim);

                                retVal.AddRange(context.Query<CompositeResult<DbSecurityPolicy, DbSecurityDevicePolicy>>(query).Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, user)).Where(p => retVal.Any(r => r.Policy.Oid == p.Policy.Oid)));
                            }

                        }

                        this.m_traceSource.TraceEvent(TraceEventType.Verbose, 0, "Principal {0} effective policy set {1}", user.UserName, String.Join(",", retVal.Select(o => $"{o.Policy.Oid} [{o.Rule}]")));
                        // TODO: Most restrictive
                        return retVal;
                    }
                    else if (securable is Core.Model.Acts.Act)
                    {
                        var pAct = securable as Core.Model.Acts.Act;
                        var query = context.CreateSqlStatement<DbActSecurityPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbActSecurityPolicy))
                                  .InnerJoin<DbSecurityPolicy, DbActSecurityPolicy>()
                                  .Where(o => o.SourceKey == pAct.Key);

                        return context.Query<CompositeResult<DbSecurityPolicy, DbActSecurityPolicy>>(query)
                            .Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable));
                    }
                    else if (securable is Core.Model.Entities.Entity)
                    {
                        var pEntity = securable as Core.Model.Entities.Entity;
                        var query = context.CreateSqlStatement<DbEntitySecurityPolicy>().SelectFrom(typeof(DbSecurityPolicy), typeof(DbEntitySecurityPolicy))
                                  .InnerJoin<DbSecurityPolicy, DbEntitySecurityPolicy>()
                                  .Where(o => o.SourceKey == pEntity.Key);

                        return context.Query<CompositeResult<DbSecurityPolicy, DbEntitySecurityPolicy>>(query)
                            .Select(o => new AdoSecurityPolicyInstance(o.Object2, o.Object1, securable));
                    }
                    else
                        return new List<IPolicyInstance>();
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error getting active policies for {0} : {1}", securable, e);
                    throw;
                }
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
                    return dataContext.Query<DbSecurityPolicy>(o => o.ObsoletionTime == null).Select(o => new AdoSecurityPolicy(o)).ToArray();
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error getting policies: {0}", e);
                    throw;
                }
        }

        /// <summary>
        /// Get a specific policy
        /// </summary>
        public IPolicy GetPolicy(string policyOid)
        {
            using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    dataContext.Open();
                    var policy = dataContext.SingleOrDefault<DbSecurityPolicy>(o => o.Oid == policyOid);
                    if (policy != null)
                        return new AdoSecurityPolicy(policy);
                    return null;
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error getting policy {0} : {1}", policyOid, e);
                    throw;
                }
            }

        }
    }
}
