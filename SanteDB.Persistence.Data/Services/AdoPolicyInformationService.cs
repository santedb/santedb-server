/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2022-9-7
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using SanteDB.Persistence.Data.Services.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// A PIP service which stores data in the database
    /// </summary>
    public class AdoPolicyInformationService : IPolicyInformationService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO Policy Information";

        // PEP
        private readonly IPolicyEnforcementService m_policyEnforcement;

        // Configuration
        private readonly AdoPersistenceConfigurationSection m_configuration;

        // Adhoc
        private readonly IAdhocCacheService m_adhocCache;

        // Localization service
        private readonly ILocalizationService m_localizationService;
        private readonly IAdoPersistenceProvider<Act> m_actPersistence;

        /// <summary>
        /// Trace source
        /// </summary>
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(AdoPolicyInformationService));
        private readonly IAdoPersistenceProvider<Entity> m_entityPersistence;

        /// <summary>
        /// Create new policy info service
        /// </summary>
        public AdoPolicyInformationService(IConfigurationManager configurationManager, IPolicyEnforcementService pepService, ILocalizationService localizationService, IDataPersistenceService<Entity> entityPersistence, IDataPersistenceService<Act> actPersistence, IAdhocCacheService adhocCache = null)
        {
            this.m_policyEnforcement = pepService;
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_adhocCache = adhocCache;
            this.m_localizationService = localizationService;
            this.m_actPersistence = actPersistence as IAdoPersistenceProvider<Act>;
            this.m_entityPersistence = entityPersistence as IAdoPersistenceProvider<Entity>;
        }

        /// <summary>
        /// Add policies to the specified object
        /// </summary>
        /// <param name="securable">The object to which the policies should be added</param>
        /// <param name="rule">The rule on the object</param>
        /// <param name="principal">The principal whihc is adding the rule</param>
        /// <param name="policyOids">The policy OIDs to add</param>
        public void AddPolicies(object securable, PolicyGrantType rule, IPrincipal principal, params string[] policyOids)
        {
            if (securable == null)
            {
                throw new ArgumentNullException(nameof(securable), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    context.EstablishProvenance(principal, null);

                    using (var tx = context.BeginTransaction())
                    {
                        var policies = context.Query<DbSecurityPolicy>(o => policyOids.Contains(o.Oid)).Select(o => o.Key).ToArray();

                        this.Demand(securable, principal);

                        switch (securable)
                        {
                            case SecurityRole sr:
                                {
                                    context.DeleteAll<DbSecurityRolePolicy>(o => policies.Contains(o.PolicyKey) && o.SourceKey == sr.Key && policies.Contains(o.PolicyKey));
                                    context.InsertAll(policies.Select(o => new DbSecurityRolePolicy()
                                    {
                                        GrantType = (int)rule,
                                        SourceKey = sr.Key.Value,
                                        PolicyKey = o
                                    }));
                                    break;
                                }
                            case IApplicationIdentity iaid:
                                {
                                    var sid = context.Query<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == iaid.Name.ToLowerInvariant()).Select(o=>o.Key).First();
                                    context.DeleteAll<DbSecurityApplicationPolicy>(o => policies.Contains(o.PolicyKey) && o.SourceKey == sid && policies.Contains(o.PolicyKey));
                                    context.InsertAll(policies.Select(o => new DbSecurityApplicationPolicy()
                                    {
                                        GrantType = (int)rule,
                                        SourceKey = sid,
                                        PolicyKey = o
                                    }));
                                    break;
                                }
                            case SecurityApplication sa:
                                {
                                    context.DeleteAll<DbSecurityApplicationPolicy>(o => policies.Contains(o.PolicyKey) && o.SourceKey == sa.Key && policies.Contains(o.PolicyKey));
                                    context.InsertAll(policies.Select(o => new DbSecurityApplicationPolicy()
                                    {
                                        GrantType = (int)rule,
                                        SourceKey = sa.Key.Value,
                                        PolicyKey = o
                                    }));
                                    break;
                                }
                            case IDeviceIdentity idid:
                                {
                                    var sid = context.Query<DbSecurityDevice>(o => o.PublicId.ToLowerInvariant() == idid.Name.ToLowerInvariant()).Select(o => o.Key).First();
                                    context.DeleteAll<DbSecurityDevicePolicy>(o => policies.Contains(o.PolicyKey) && o.SourceKey == sid && policies.Contains(o.PolicyKey));
                                    context.InsertAll(policies.Select(o => new DbSecurityDevicePolicy()
                                    {
                                        GrantType = (int)rule,
                                        SourceKey = sid,
                                        PolicyKey = o
                                    }));
                                    break;
                                }
                            case SecurityDevice sd:
                                {
                                    context.DeleteAll<DbSecurityDevicePolicy>(o => policies.Contains(o.PolicyKey) && o.SourceKey == sd.Key && policies.Contains(o.PolicyKey));
                                    context.InsertAll(policies.Select(o => new DbSecurityDevicePolicy()
                                    {
                                        GrantType = (int)rule,
                                        SourceKey = sd.Key.Value,
                                        PolicyKey = o
                                    }));
                                    break;
                                }
                            case Entity entity:
                                {

                                    context.UpdateAll<DbEntitySecurityPolicy>(o => o.SourceKey == entity.Key && policies.Contains(o.PolicyKey), o=>o.ObsoleteVersionSequenceId == entity.VersionSequence);
                                    // Touch the entity version
                                    entity = this.m_entityPersistence.Touch(context, entity.Key.Value);
                                    context.InsertAll(policies.Select(o => new DbEntitySecurityPolicy()
                                    {
                                        EffectiveVersionSequenceId = entity.VersionSequence.Value,
                                        PolicyKey = o,
                                        SourceKey = entity.Key.Value
                                    }));
                                    break;
                                }
                            case Act act:
                                {
                                    context.UpdateAll<DbActSecurityPolicy>(o => o.SourceKey == act.Key && policies.Contains(o.PolicyKey), o=> o.ObsoleteVersionSequenceId == act.VersionSequence);
                                    act = this.m_actPersistence.Touch(context, act.Key.Value);
                                    context.InsertAll(policies.Select(o => new DbActSecurityPolicy()
                                    {
                                        SourceKey = act.Key.Value,
                                        EffectiveVersionSequenceId = act.VersionSequence.Value,
                                        PolicyKey = o
                                    }));
                                    break;
                                }
                            default:
                                {
                                    throw new NotSupportedException(this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_NOT_SUPPORTED));
                                }
                        }
                        tx.Commit();

                        if (securable is IdentifiedData id)
                        {
                            this.m_adhocCache?.Remove($"pip.{id.Type}.{id.Key}.{id.Tag}");
                        }
                    }
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Error padding policies to {0} : {1}", securable, e);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_ASSIGN, new { target = securable, policyOids = String.Join(",", policyOids) }), e);
                }
            }
        }

        /// <summary>
        /// Demand appropriate permission
        /// </summary>
        private void Demand(object securable, IPrincipal principal)
        {
            if (securable is SecurityRole ||
                securable is SecurityApplication ||
                securable is SecurityDevice sd)
            {
                this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.AssignPolicy, principal);
            }
            else if (securable is Entity entity)
            {
                switch (entity.Type)
                {
                    case "Patient":
                    case "Person":
                    case "Provider":
                        this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.WriteClinicalData, principal);
                        break;

                    case "Place":
                    case "State":
                    case "County":
                    case "City":
                    case "Organization":
                        this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.WritePlacesAndOrgs, principal);
                        break;

                    case "ManufacturedMaterial":
                    case "Material":
                        this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.WriteMaterials, principal);
                        break;
                }

                this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.AssignPolicy, principal);
            }
            else if (securable is Act)
            {
                this.m_policyEnforcement.Demand(PermissionPolicyIdentifiers.WriteClinicalData, principal);
            }
        }

        /// <summary>
        /// Return true if the specified object has the specified policy
        /// </summary>
        public bool HasPolicy(object securable, String policyId)
        {
            if (securable == null)
            {
                throw new ArgumentNullException(nameof(securable), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (String.IsNullOrEmpty(policyId))
            {
                throw new ArgumentNullException(nameof(policyId), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();

                    // Security Device
                    if (securable is SecurityDevice sd)
                    {
                        return context.Any(
                            context.CreateSqlStatement<DbSecurityDevicePolicy>()
                                .SelectFrom(typeof(DbSecurityDevicePolicy), typeof(DbSecurityPolicy))
                            .InnerJoin<DbSecurityPolicy, DbSecurityDevicePolicy>(o => o.Key, o => o.PolicyKey)
                            .Where<DbSecurityDevicePolicy>(o => o.SourceKey == sd.Key));
                    }
                    else if (securable is SecurityApplication sa)
                    {
                        return context.Any(
                            context.CreateSqlStatement<DbSecurityApplicationPolicy>()
                                .SelectFrom(typeof(DbSecurityApplicationPolicy), typeof(DbSecurityPolicy))
                            .InnerJoin<DbSecurityPolicy, DbSecurityApplicationPolicy>(o => o.Key, o => o.PolicyKey)
                            .Where<DbSecurityApplicationPolicy>(o => o.SourceKey == sa.Key));
                    }
                    else if (securable is SecurityRole sr)
                    {
                        return context.Any(
                            context.CreateSqlStatement<DbSecurityRolePolicy>()
                                .SelectFrom(typeof(DbSecurityRolePolicy), typeof(DbSecurityPolicy))
                            .InnerJoin<DbSecurityPolicy, DbSecurityRolePolicy>(o => o.Key, o => o.PolicyKey)
                            .Where<DbSecurityRolePolicy>(o => o.SourceKey == sr.Key));
                    }
                    else if (securable is Entity entity)
                    {
                        return context.Any(
                            context.CreateSqlStatement<DbEntitySecurityPolicy>()
                                .SelectFrom(typeof(DbEntitySecurityPolicy), typeof(DbSecurityPolicy))
                            .InnerJoin<DbSecurityPolicy, DbEntitySecurityPolicy>(o => o.Key, o => o.PolicyKey)
                                .Where<DbEntitySecurityPolicy>(o => o.SourceKey == entity.Key && o.ObsoleteVersionSequenceId == null));
                    }
                    else if (securable is Act act)
                    {
                        return context.Any(
                            context.CreateSqlStatement<DbActSecurityPolicy>()
                                .SelectFrom(typeof(DbActSecurityPolicy), typeof(DbSecurityPolicy))
                            .InnerJoin<DbSecurityPolicy, DbActSecurityPolicy>(o => o.Key, o => o.PolicyKey)
                                .Where<DbActSecurityPolicy>(o => o.SourceKey == act.Key && o.ObsoleteVersionSequenceId == null));
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(securable), this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_NOT_SUPPORTED));
                    }
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Error getting active policies for {0} : {1}", securable, e);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_GEN), e);
                }
            }
        }

        /// <summary>
        /// Get all policy instances for the specified securable
        /// </summary>
        /// <param name="securable"></param>
        /// <returns></returns>
        public IEnumerable<IPolicyInstance> GetPolicies(object securable)
        {
            if (securable == null)
            {
                throw new ArgumentNullException(nameof(securable), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            IEnumerable<AdoSecurityPolicyInstance> results = null;
            if (securable is IdentifiedData id)
            {
                results = this.m_adhocCache?.Get<AdoSecurityPolicyInstance[]>($"pip.{id.Type}.{id.Key}.{id.Tag}");
            }

            if (results == null) // Load from DB
            {
                using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    try
                    {
                        context.Open();

                        // Security Device
                        switch (securable)
                        {
                            case SecurityDevice sd:
                                {
                                    results = context.Query<CompositeResult<DbSecurityDevicePolicy, DbSecurityPolicy>>(
                                        context.CreateSqlStatement<DbSecurityDevicePolicy>()
                                            .SelectFrom(typeof(DbSecurityDevicePolicy), typeof(DbSecurityPolicy))
                                        .InnerJoin<DbSecurityDevicePolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                                        .Where<DbSecurityDevicePolicy>(o => o.SourceKey == sd.Key))
                                        .ToArray()
                                        .Select(o => new AdoSecurityPolicyInstance(o.Object1, o.Object2, securable));
                                    break;
                                }
                            case SecurityApplication sa:
                                {
                                    results = context.Query<CompositeResult<DbSecurityApplicationPolicy, DbSecurityPolicy>>(
                                        context.CreateSqlStatement<DbSecurityApplicationPolicy>()
                                            .SelectFrom(typeof(DbSecurityApplicationPolicy), typeof(DbSecurityPolicy))
                                        .InnerJoin<DbSecurityApplicationPolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                                        .Where<DbSecurityApplicationPolicy>(o => o.SourceKey == sa.Key))
                                        .ToArray()
                                        .Select(o => new AdoSecurityPolicyInstance(o.Object1, o.Object2, securable));
                                    break;
                                }
                            case SecurityRole sr:
                                {
                                    results = context.Query<CompositeResult<DbSecurityRolePolicy, DbSecurityPolicy>>(
                                        context.CreateSqlStatement<DbSecurityRolePolicy>()
                                            .SelectFrom(typeof(DbSecurityRolePolicy), typeof(DbSecurityPolicy))
                                        .InnerJoin<DbSecurityRolePolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                                        .Where<DbSecurityRolePolicy>(o => o.SourceKey == sr.Key))
                                        .ToArray()
                                        .Select(o => new AdoSecurityPolicyInstance(o.Object1, o.Object2, securable));
                                    break;
                                }
                            case IIdentity identity:
                                {
                                    results = this.GetIdentityPolicies(context, identity, new String[0]).ToArray();
                                    break;
                                }
                            case IPrincipal principal:
                                {
                                    var primaryId = principal.Identity;

                                    results = this.GetIdentityPolicies(context, primaryId, new String[0]).ToArray();

                                    // Secondary identities
                                    if (principal is IClaimsPrincipal claimsPrincipal)
                                    {
                                        foreach (var subId in claimsPrincipal.Identities.Where(o => o != primaryId))
                                        {
                                            results = results.Union(this.GetIdentityPolicies(context, subId, results.Select(o => o.Policy.Oid))).ToArray();
                                        }
                                    }
                                    break;
                                }
                            case Entity entity:
                                {
                                    results = context.Query<CompositeResult<DbEntitySecurityPolicy, DbSecurityPolicy>>(
                                        context.CreateSqlStatement<DbEntitySecurityPolicy>()
                                            .SelectFrom(typeof(DbEntitySecurityPolicy), typeof(DbSecurityPolicy))
                                        .InnerJoin<DbEntitySecurityPolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                                            .Where<DbEntitySecurityPolicy>(o => o.SourceKey == entity.Key && o.ObsoleteVersionSequenceId == null))
                                        .ToArray()
                                        .Select(o => new AdoSecurityPolicyInstance(o.Object1, o.Object2, entity));
                                    break;
                                }
                            case Act act:
                                {
                                    results = context.Query<CompositeResult<DbActSecurityPolicy, DbSecurityPolicy>>(
                                        context.CreateSqlStatement<DbActSecurityPolicy>()
                                            .SelectFrom(typeof(DbActSecurityPolicy), typeof(DbSecurityPolicy))
                                        .InnerJoin<DbActSecurityPolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                                            .Where<DbActSecurityPolicy>(o => o.SourceKey == act.Key && o.ObsoleteVersionSequenceId == null))
                                        .ToArray()
                                        .Select(o => new AdoSecurityPolicyInstance(o.Object1, o.Object2, act));
                                    break;
                                }
                            default:
                                {
                                    results = new List<AdoSecurityPolicyInstance>();
                                    break;
                                }
                        }

                        if (securable is IdentifiedData id1)
                        {
                            this.m_adhocCache?.Add($"pip.{id1.Type}.{id1.Key}.{id1.Tag}", results, new TimeSpan(0, 5, 0));
                        }
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error getting active policies for {0} : {1}", securable, e);
                        throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_GEN), e);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Get identity policies
        /// </summary>
        private IEnumerable<AdoSecurityPolicyInstance> GetIdentityPolicies(DataContext context, IIdentity identity, IEnumerable<String> filterOids)
        {
            IEnumerable<CompositeResult<DbSecurityPolicyActionableInstance, DbSecurityPolicy>> retVal = null;
            // Establish baseline
            if (identity is IApplicationIdentity appId)
            {
                retVal = context.Query<CompositeResult<DbSecurityPolicyActionableInstance, DbSecurityPolicy>>(
                    context.CreateSqlStatement<DbSecurityApplicationPolicy>()
                        .SelectFrom(typeof(DbSecurityApplicationPolicy), typeof(DbSecurityPolicy))
                        .InnerJoin<DbSecurityApplicationPolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                        .InnerJoin<DbSecurityApplicationPolicy, DbSecurityApplication>(o => o.SourceKey, o => o.Key)
                        .Where<DbSecurityApplication>(o => o.PublicId.ToLowerInvariant() == appId.Name.ToLowerInvariant())
                    );
            }
            else if (identity is IDeviceIdentity devId)
            {
                retVal = context.Query<CompositeResult<DbSecurityPolicyActionableInstance, DbSecurityPolicy>>(
                    context.CreateSqlStatement<DbSecurityDevicePolicy>()
                        .SelectFrom(typeof(DbSecurityDevicePolicy), typeof(DbSecurityPolicy))
                        .InnerJoin<DbSecurityDevicePolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                        .InnerJoin<DbSecurityDevicePolicy, DbSecurityDevice>(o => o.SourceKey, o => o.Key)
                        .Where<DbSecurityDevice>(o => o.PublicId.ToLowerInvariant() == devId.Name.ToLowerInvariant())
                        );
            }
            else
            {
                retVal = context.Query<CompositeResult<DbSecurityPolicyActionableInstance, DbSecurityPolicy>>(
                    context.CreateSqlStatement<DbSecurityRolePolicy>()
                        .SelectFrom(typeof(DbSecurityRolePolicy), typeof(DbSecurityPolicy))
                        .InnerJoin<DbSecurityRolePolicy, DbSecurityPolicy>(o => o.PolicyKey, o => o.Key)
                        .InnerJoin<DbSecurityRolePolicy, DbSecurityUserRole>(o => o.SourceKey, o => o.RoleKey)
                        .InnerJoin<DbSecurityUserRole, DbSecurityUser>(o => o.UserKey, o => o.Key)
                        .Where<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == identity.Name.ToLowerInvariant())
                    );
            }

            if (filterOids.Any())
            {
                retVal = retVal.ToArray().Where(o => filterOids.Contains(o.Object2.Oid));
            }

            return retVal.Select(o => new AdoSecurityPolicyInstance(o.Object1, o.Object2, identity));
        }

        /// <summary>
        /// Get all policies for the PIP system
        /// </summary>
        public IEnumerable<IPolicy> GetPolicies()
        {
            using (var dataContext = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    dataContext.Open();
                    return dataContext.Query<DbSecurityPolicy>(o => o.ObsoletionTime == null).ToArray().Select(o => new AdoSecurityPolicy(o)).ToArray();
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Error getting policies: {0}", e);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_GEN), e);
                }
            }
        }

        /// <summary>
        /// Get the policy from the PIP
        /// </summary>
        public IPolicy GetPolicy(string policyOid)
        {
            if (String.IsNullOrEmpty(policyOid))
            {
                throw new ArgumentNullException(nameof(policyOid), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            // Cache hit
            var retVal = this.m_adhocCache?.Get<DbSecurityPolicy>($"pip.{policyOid}");
            if (retVal != null)
            {
                return new AdoSecurityPolicy(retVal);
            }

            using (var context = this.m_configuration.Provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    retVal = context.SingleOrDefault<DbSecurityPolicy>(o => o.Oid == policyOid && o.ObsoletionTime == null);

                    this.m_adhocCache?.Add($"pip.{policyOid}", retVal, new TimeSpan(0, 0, 30));

                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Error fetching {0} : {1}", policyOid, e);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_GEN), e);
                }
            }

            if (retVal != null)
            {
                return new AdoSecurityPolicy(retVal);
            }
            return null;
        }

        /// <summary>
        /// Get the specified policy instance for the specified object
        /// </summary>
        /// <param name="securable">The securable to fetch</param>
        /// <param name="policyOid">The policy OID</param>
        /// <returns>The policy instance</returns>
        public IPolicyInstance GetPolicyInstance(object securable, string policyOid)
        {
            return this.GetPolicies(securable).FirstOrDefault(o => o.Policy.Oid == policyOid);
        }

        /// <summary>
        /// Remove the <paramref name="oid"/> policies from <paramref name="securable"/>
        /// </summary>
        /// <param name="securable">The securable from which data should be removed</param>
        /// <param name="principal">The principal which is removing them</param>
        /// <param name="oid">The policies to be removed</param>
        public void RemovePolicies(object securable, IPrincipal principal, params string[] oid)
        {
            if (securable == null)
            {
                throw new ArgumentNullException(nameof(securable), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal), this.m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    context.Open();
                    using (var tx = context.BeginTransaction())
                    {
                        var policies = context.Query<DbSecurityPolicy>(o => oid.Contains(o.Oid)).Select(o => o.Key).ToArray();

                        this.Demand(securable, principal);

                        if (securable is SecurityRole sr)
                        {
                            context.DeleteAll<DbSecurityRolePolicy>(o => policies.Contains(o.PolicyKey) && o.SourceKey == sr.Key && policies.Contains(o.PolicyKey));
                        }
                        else if (securable is SecurityApplication sa)
                        {
                            context.DeleteAll<DbSecurityApplicationPolicy>(o => policies.Contains(o.PolicyKey) && o.SourceKey == sa.Key && policies.Contains(o.PolicyKey));
                        }
                        else if (securable is SecurityDevice sd)
                        {
                            context.DeleteAll<DbSecurityDevicePolicy>(o => policies.Contains(o.PolicyKey) && o.SourceKey == sd.Key && policies.Contains(o.PolicyKey));
                        }
                        else if (securable is Entity entity)
                        {
                            context.DeleteAll<DbEntitySecurityPolicy>(o => o.SourceKey == entity.Key && policies.Contains(o.PolicyKey));
                        }
                        else if (securable is Act act)
                        {
                            context.DeleteAll<DbActSecurityPolicy>(o => o.SourceKey == act.Key && policies.Contains(o.PolicyKey));
                        }
                        else
                        {
                            throw new NotSupportedException(this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_NOT_SUPPORTED));
                        }
                        tx.Commit();

                        if (securable is IdentifiedData id)
                        {
                            this.m_adhocCache?.Remove($"pip.{id.GetType().Name}.{id.Key}.{id.Tag}");
                        }
                    }
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Error padding policies to {0} : {1}", securable, e);
                    throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.SEC_POL_ASSIGN, new { securable = securable, policyOids = String.Join(";", oid) }), e);
                }
            }
        }
    }
}