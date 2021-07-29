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
        public AdoPersistenceService(IServiceManager serviceManager)
        {
            var tracer = new Tracer(AdoDataConstants.TraceSourceName);

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

        /// <summary>
        /// Generic versioned persister service for any non-customized persister
        /// </summary>
        internal class GenericBasePersistenceService<TModel, TDomain> : BaseDataPersistenceService<TModel, TDomain>
            where TDomain : class, IDbBaseData, new()
            where TModel : BaseEntityData, new()
        {

            public GenericBasePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Ensure exists
            /// </summary>
            public override TModel InsertInternal(DataContext context, TModel data)
            {
                foreach (var rp in typeof(TModel).GetRuntimeProperties().Where(o => typeof(IdentifiedData).IsAssignableFrom(o.PropertyType)))
                {
                    if (rp.GetCustomAttribute<DataIgnoreAttribute>() != null || !rp.CanRead || !rp.CanWrite)
                        continue;

                    var instance = rp.GetValue(data);
                    if (instance != null)
                        rp.SetValue(data, DataModelExtensions.EnsureExists(instance as IdentifiedData, context));
                }
                return base.InsertInternal(context, data);
            }

            /// <summary>
            /// Update the specified object
            /// </summary>
            public override TModel UpdateInternal(DataContext context, TModel data)
            {
                foreach (var rp in typeof(TModel).GetRuntimeProperties().Where(o => typeof(IdentifiedData).IsAssignableFrom(o.PropertyType)))
                {
                    if (rp.GetCustomAttribute<DataIgnoreAttribute>() != null || !rp.CanRead || !rp.CanWrite)
                        continue;

                    var instance = rp.GetValue(data);
                    if (instance != null)
                        rp.SetValue(data, DataModelExtensions.EnsureExists(instance as IdentifiedData, context));

                }
                return base.UpdateInternal(context, data);
            }
        }

        /// <summary>
        /// Generic versioned persister service for any non-customized persister
        /// </summary>
        internal class GenericIdentityPersistenceService<TModel, TDomain> : IdentifiedPersistenceService<TModel, TDomain>
            where TModel : IdentifiedData, new()
            where TDomain : class, IDbIdentified, new()
        {

            public GenericIdentityPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Ensure exists
            /// </summary>
            public override TModel InsertInternal(DataContext context, TModel data)
            {
                foreach (var rp in typeof(TModel).GetRuntimeProperties().Where(o => typeof(IdentifiedData).IsAssignableFrom(o.PropertyType)))
                {
                    if (rp.GetCustomAttribute<DataIgnoreAttribute>() != null || !rp.CanRead || !rp.CanWrite)
                        continue;

                    var instance = rp.GetValue(data);
                    if (instance != null)
                        rp.SetValue(data, DataModelExtensions.EnsureExists(instance as IdentifiedData, context));

                }
                return base.InsertInternal(context, data);
            }

            /// <summary>
            /// Update the specified object
            /// </summary>
            public override TModel UpdateInternal(DataContext context, TModel data)
            {
                foreach (var rp in typeof(TModel).GetRuntimeProperties().Where(o => typeof(IdentifiedData).IsAssignableFrom(o.PropertyType)))
                {
                    if (rp.GetCustomAttribute<DataIgnoreAttribute>() != null || !rp.CanRead || !rp.CanWrite)
                        continue;

                    var instance = rp.GetValue(data);
                    if (instance != null)
                        rp.SetValue(data, DataModelExtensions.EnsureExists(instance as IdentifiedData, context));

                }
                return base.UpdateInternal(context, data);
            }
        }

        /// <summary>
        /// Generic association persistence service
        /// </summary>
        internal class GenericBaseAssociationPersistenceService<TModel, TDomain> :
            GenericBasePersistenceService<TModel, TDomain>, IAdoAssociativePersistenceService
            where TModel : BaseEntityData, ISimpleAssociation, new()
            where TDomain : class, IDbBaseData, new()
        {

            public GenericBaseAssociationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Get all the matching TModel object from source
            /// </summary>
            public IEnumerable GetFromSource(DataContext context, Guid sourceId, decimal? versionSequenceId)
            {
                int tr = 0;
                return this.QueryInternal(context, base.BuildSourceQuery<TModel>(sourceId), Guid.Empty, 0, null, out tr, null).ToList();
            }
        }

        /// <summary>
        /// Generic association persistence service
        /// </summary>
        internal class GenericBaseVersionedAssociationPersistenceService<TModel, TDomain> :
            GenericBasePersistenceService<TModel, TDomain>, IAdoAssociativePersistenceService
            where TModel : BaseEntityData, IVersionedAssociation, new()
            where TDomain : class, IDbBaseData, new()
        {

            public GenericBaseVersionedAssociationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Get all the matching TModel object from source
            /// </summary>
            public IEnumerable GetFromSource(DataContext context, Guid sourceId, decimal? versionSequenceId)
            {
                int tr = 0;
                // TODO: Check that this query is actually building what it is supposed to.
                return this.QueryInternal(context, base.BuildSourceQuery<TModel>(sourceId, versionSequenceId), Guid.Empty, 0, null, out tr, null).ToList();
            }
        }

        /// <summary>
        /// Generic association persistence service
        /// </summary>
        internal class GenericIdentityAssociationPersistenceService<TModel, TDomain> :
            GenericIdentityPersistenceService<TModel, TDomain>, IAdoAssociativePersistenceService
            where TModel : IdentifiedData, ISimpleAssociation, new()
            where TDomain : class, IDbIdentified, new()
        {


            public GenericIdentityAssociationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Get all the matching TModel object from source
            /// </summary>
            public IEnumerable GetFromSource(DataContext context, Guid sourceId, decimal? versionSequenceId)
            {
                int tr = 0;
                return this.QueryInternal(context, base.BuildSourceQuery<TModel>(sourceId), Guid.Empty, 0, null, out tr, null).ToList();
            }
        }

        /// <summary>
        /// Generic association persistence service
        /// </summary>
        internal class GenericIdentityVersionedAssociationPersistenceService<TModel, TDomain> :
            GenericIdentityPersistenceService<TModel, TDomain>, IAdoAssociativePersistenceService
            where TModel : IdentifiedData, IVersionedAssociation, new()
            where TDomain : class, IDbIdentified, new()
        {

            public GenericIdentityVersionedAssociationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
            {
            }

            /// <summary>
            /// Get all the matching TModel object from source
            /// </summary>
            public IEnumerable GetFromSource(DataContext context, Guid sourceId, decimal? versionSequenceId)
            {
                int tr = 0;
                // TODO: Check that this query is actually building what it is supposed to.
                return this.QueryInternal(context, base.BuildSourceQuery<TModel>(sourceId, versionSequenceId), Guid.Empty, 0, null, out tr, null).ToList();
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

                // Apply the migrations
                this.m_tracer.TraceInfo("Scanning for schema updates...");

                // TODO: Refactor this to a common library within the ORM tooling
                this.m_configuration.Provider.UpgradeSchema("SanteDB.Persistence.Data.ADO");

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
                            this.GetConfiguration().AllowedResources.Contains(t.GetGenericArguments()[0].GetCustomAttribute<XmlTypeAttribute>()?.TypeName)))
                        {
                            var instance = Activator.CreateInstance(t, this);
                            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(instance);
                        }

                        // Add to cache since we're here anyways

                        //s_persistenceCache.Add(t.GetGenericArguments()[0], Activator.CreateInstance(t) as IAdoPersistenceService);
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceEvent(EventLevel.Error, "Error adding service {0} : {1}", t.AssemblyQualifiedName, e);
                        throw new InvalidOperationException($"Error adding service {t.AssemblyQualifiedName}", e);
                    }
                }

                // Now iterate through the map file and ensure we have all the mappings, if a class does not exist create it
                try
                {
                    this.m_tracer.TraceEvent(EventLevel.Informational, "Creating secondary model maps...");

                    var map = ModelMap.Load(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(AdoDataConstants.MapResourceName));
                    foreach (var itm in map.Class)
                    {
                        // Is there a persistence service?
                        var idpType = typeof(IDataPersistenceService<>);
                        Type modelClassType = Type.GetType(itm.ModelClass),
                            domainClassType = Type.GetType(itm.DomainClass);

                        // Make sure we're allowed to run this
                        if (this.GetConfiguration().AllowedResources.Count > 0 &&
                            !this.GetConfiguration().AllowedResources.Contains(modelClassType.GetCustomAttribute<XmlTypeAttribute>()?.TypeName))
                            continue;

                        idpType = idpType.MakeGenericType(modelClassType);

                        if (modelClassType.IsAbstract || domainClassType.IsAbstract) continue;

                        // Already created
                        if (ApplicationServiceContext.Current.GetService(idpType) != null)
                            continue;

                        this.m_tracer.TraceEvent(EventLevel.Verbose, "Creating map {0} > {1}", modelClassType, domainClassType);

                        if (this.m_persistenceCache.ContainsKey(modelClassType))
                            this.m_tracer.TraceWarning("Duplicate initialization of {0}", modelClassType);
                        else if (modelClassType.Implements(typeof(IBaseEntityData)) &&
                           domainClassType.Implements(typeof(IDbBaseData)))
                        {
                            // Construct a type
                            Type pclass = null;
                            if (modelClassType.Implements(typeof(IVersionedAssociation)))
                                pclass = typeof(GenericBaseVersionedAssociationPersistenceService<,>);
                            else if (modelClassType.Implements(typeof(ISimpleAssociation)))
                                pclass = typeof(GenericBaseAssociationPersistenceService<,>);
                            else
                                pclass = typeof(GenericBasePersistenceService<,>);
                            pclass = pclass.MakeGenericType(modelClassType, domainClassType);
                            var instance = Activator.CreateInstance(pclass, this);
                            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(instance);
                            // Add to cache since we're here anyways
                            this.m_persistenceCache.Add(modelClassType, instance as IAdoPersistenceService);
                        }
                        else if (modelClassType.Implements(typeof(IIdentifiedEntity)) &&
                            domainClassType.Implements(typeof(IDbIdentified)))
                        {
                            // Construct a type
                            Type pclass = null;
                            if (modelClassType.Implements(typeof(IVersionedAssociation)))
                                pclass = typeof(GenericIdentityVersionedAssociationPersistenceService<,>);
                            else if (modelClassType.Implements(typeof(ISimpleAssociation)))
                                pclass = typeof(GenericIdentityAssociationPersistenceService<,>);
                            else
                                pclass = typeof(GenericIdentityPersistenceService<,>);

                            pclass = pclass.MakeGenericType(modelClassType, domainClassType);
                            var instance = Activator.CreateInstance(pclass, this);
                            ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(instance);
                            this.m_persistenceCache.Add(modelClassType, instance as IAdoPersistenceService);
                        }
                        else
                            this.m_tracer.TraceEvent(EventLevel.Warning, "Classmap {0}>{1} cannot be created, ignoring", modelClassType, domainClassType);

                    }
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceEvent(EventLevel.Error, "Error initializing local persistence: {0}", e);
                    throw new Exception("Error initializing local persistence", e);
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
                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, PermissionPolicyIdentifiers.UnrestrictedAdministration).Demand();

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

