/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using SanteDB.BI;
using SanteDB.BI.Model;
using SanteDB.BI.Services;
using SanteDB.BI.Util;
using SanteDB.Core;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Migration;
using SanteDB.OrmLite.Providers;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Configuration.Features;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Hax;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Services.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a dummy service which just adds the persistence services to the context
    /// </summary>
    [ServiceProvider("ADO.NET Data Persistence Service", Configuration = typeof(AdoPersistenceConfigurationSection))]
    public class AdoPersistenceService : IDaemonService, ISqlDataPersistenceService, IAdoPersistenceSettingsProvider
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Data Persistence Service";

        private ModelMapper m_mapper;

        private AdoPersistenceConfigurationSection m_configuration;

        // Cache
        private Dictionary<Type, IAdoPersistenceService> m_persistenceCache = new Dictionary<Type, IAdoPersistenceService>();

        // Query builder
        private QueryBuilder m_queryBuilder;

        // Policy enforcement service
        private IPolicyEnforcementService m_policyEnforcementService;

        /// <summary>
        /// Get configuration
        /// </summary>
        public AdoPersistenceConfigurationSection GetConfiguration()
        {
            if (m_configuration == null)
                m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();
            return m_configuration;
        }

        /// <summary>
        /// Gets the mode mapper
        /// </summary>
        /// <returns></returns>
        public ModelMapper GetMapper() { return this.m_mapper; }

        /// <summary>
        /// Get query builder
        /// </summary>
        public QueryBuilder GetQueryBuilder()
        {
            return this.m_queryBuilder;
        }

        /// <summary>
        /// Get the specified persister type
        /// </summary>
        public IAdoPersistenceService GetPersister(Type tDomain)
        {
            IAdoPersistenceService retVal = null;
            if (!this.m_persistenceCache.TryGetValue(tDomain, out retVal))
            {
                // Scan type heirarchy as well
                var sDomain = tDomain;
                var idpType = typeof(IDataPersistenceService<>).MakeGenericType(sDomain);
                retVal = ApplicationServiceContext.Current.GetService(idpType) as IAdoPersistenceService;
                while (retVal == null && sDomain != typeof(object))
                {
                    idpType = typeof(IDataPersistenceService<>).MakeGenericType(sDomain);
                    retVal = ApplicationServiceContext.Current.GetService(idpType) as IAdoPersistenceService;
                    sDomain = sDomain.BaseType;
                }

                if (retVal != null)
                    lock (this.m_persistenceCache)
                        if (!this.m_persistenceCache.ContainsKey(tDomain))
                            this.m_persistenceCache.Add(tDomain, retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Creates a new instance of the ADO cache
        /// </summary>
        public AdoPersistenceService(IServiceManager serviceManager, IPolicyEnforcementService policyEnforcementService)
        {
            this.m_policyEnforcementService = policyEnforcementService;
            var tracer = new Tracer(AdoDataConstants.TraceSourceName);

            // Apply the migrations
            this.m_tracer.TraceInfo("Scanning for schema updates...");

            // TODO: Refactor this to a common library within the ORM tooling
            this.GetConfiguration().Provider.UpgradeSchema("SanteDB.Persistence.Data.ADO");

            try
            {
                this.m_mapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(AdoDataConstants.MapResourceName), AdoDataConstants.ModelMapName);

                List<IQueryBuilderHack> hax = new List<IQueryBuilderHack>() { new SecurityUserEntityQueryHack(), new RelationshipGuardQueryHack(), new CreationTimeQueryHack(this.m_mapper), new EntityAddressNameQueryHack() };
                if (this.GetConfiguration().DataCorrectionKeys.Any(k => k == "ConceptQueryHack"))
                    hax.Add(new ConceptQueryHack(this.m_mapper));

                this.m_queryBuilder = new QueryBuilder(this.m_mapper, this.GetConfiguration().Provider,
                    hax.Where(o => o != null).ToArray()
                );

                // Bind subscription execution
                serviceManager.AddServiceProvider(typeof(AdoSubscriptionExecutor));
            }
            catch (ModelMapValidationException ex)
            {
                tracer.TraceEvent(EventLevel.Error, "Error validating model map: {0}", ex);
                throw ex;
            }
            catch (Exception ex)
            {
                tracer.TraceEvent(EventLevel.Error, "Error validating model map: {0}", ex);
                throw ex;
            }
        }

        // Tracer
        private Tracer m_tracer = new Tracer(AdoDataConstants.TraceSourceName);

        // When service is running
        private bool m_running = false;

        /// <summary>
        /// Service is starting
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Service has started
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Service has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// True when the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_running;
            }
        }

        /// <summary>
        /// Gets the invariant name
        /// </summary>
        public string InvariantName => this.GetConfiguration().Provider.Invariant;

        /// <summary>
        /// Start the service and bind all of the sub-services
        /// </summary>
        public bool Start()
        {
            // Startup on system
            using (AuthenticationContext.EnterSystemContext())
            {
                // notify startup
                this.Starting?.Invoke(this, EventArgs.Empty);
                if (this.m_running) return true;

                try
                {
                    // Verify schema version
                    using (DataContext mdc = this.GetConfiguration().Provider.GetReadonlyConnection())
                    {
                        mdc.Open();
                        Version dbVer = new Version(mdc.ExecuteProcedure<String>("get_sch_vrsn")),
                            oizVer = typeof(AdoPersistenceService).Assembly.GetName().Version;

                        if (oizVer < dbVer)
                            throw new InvalidOperationException(String.Format("Invalid Schema Version. SanteDB version {0} is older than the database schema version {1}", oizVer, dbVer));
                        this.m_tracer.TraceInfo("SanteDB Schema Version {0} on {1}", dbVer, this.GetConfiguration().Provider.Invariant);
                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, "Error starting ADO provider: {0}", e);
                    throw new InvalidOperationException("Could not start up ADO provider", e);
                }

                // Iterate the persistence services
                foreach (var t in typeof(AdoPersistenceService).Assembly.ExportedTypes.Where(o => o.Namespace == "SanteDB.Persistence.Data.ADO.Services.Persistence" && !o.IsAbstract && !o.IsGenericTypeDefinition))
                {
                    try
                    {
                        this.m_tracer.TraceEvent(EventLevel.Informational, "Loading {0}...", t.AssemblyQualifiedName);

                        // If the persistence service is generic then we should check if we're allowed
                        if (!t.IsGenericType ||
                            t.IsGenericType && (this.GetConfiguration().AllowedResources.Count == 0 ||
                            this.GetConfiguration().AllowedResources.Any(r => r.Type == t.GetGenericArguments()[0])))
                        {
                            var instance = Activator.CreateInstance(t, this);
                            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(instance);
                        }
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceEvent(EventLevel.Error, "Error adding service {0} : {1}", t.AssemblyQualifiedName, e);
                        throw new InvalidOperationException($"Error adding service {t.AssemblyQualifiedName}", e);
                    }
                }

                // Bind BI stuff
                ApplicationServiceContext.Current.GetService<IBiMetadataRepository>()?.Insert(new SanteDB.BI.Model.BiDataSourceDefinition()
                {
                    IsSystemObject = true,
                    MetaData = new BiMetadata()
                    {
                        Version = typeof(AdoPersistenceService).Assembly.GetName().Version.ToString(),
                        Status = BiDefinitionStatus.Active,
                    },
                    Id = "org.santedb.bi.dataSource.main",
                    Name = "main",
                    ConnectionString = this.m_configuration.ReadonlyConnectionString,
                    ProviderType = typeof(OrmBiDataProvider)
                });

                // Bind some basic service stuff
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Core.Model.Security.SecurityUser>>().Inserting += (o, e) =>
                {
                    if (String.IsNullOrEmpty(e.Data.SecurityHash))
                        e.Data.SecurityHash = Guid.NewGuid().ToString();
                };
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Core.Model.Security.SecurityUser>>().Updating += (o, e) =>
                {
                    e.Data.SecurityHash = Guid.NewGuid().ToString();
                };

                // Unload configuration when app domain unloads
                ApplicationServiceContext.Current.Stopped += (o, e) => this.m_configuration = null;

                // Attempt to cache concepts
                this.m_tracer.TraceEvent(EventLevel.Verbose, "Caching concept dictionary...");
                this.m_running = true;
                this.Started?.Invoke(this, EventArgs.Empty);

                return true;
            }
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.m_running = false;
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Execute the SQL provided
        /// </summary>
        public void ExecuteNonQuery(string sql)
        {
            if (AuthenticationContext.Current.Principal != AuthenticationContext.SystemPrincipal)
            {
                this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration);
            }

            using (var conn = this.GetConfiguration().Provider.GetWriteConnection())
            {
                IDbTransaction tx = null;
                try
                {
                    conn.Open();
                    tx = conn.BeginTransaction();
                    var rsql = sql;
                    while (rsql.Contains(";"))
                    {
                        conn.ExecuteNonQuery(conn.CreateSqlStatement(rsql.Substring(0, rsql.IndexOf(";"))));
                        rsql = rsql.Substring(rsql.IndexOf(";") + 1);
                    }

                    if (!String.IsNullOrEmpty(rsql) && !String.IsNullOrWhiteSpace(rsql))
                        conn.ExecuteNonQuery(conn.CreateSqlStatement(rsql));

                    tx.Commit();
                }
                catch (Exception e)
                {
                    tx.Rollback();
                    this.m_tracer.TraceEvent(EventLevel.Error, "Could not execute SQL: {0}", e);
#if DEBUG
                    throw new DataPersistenceException($"Error executing query {sql}", e);
#else
                    throw new DataPersistenceException("Error querying undelying storage service", e);
#endif
                }
            }
        }
    }
}