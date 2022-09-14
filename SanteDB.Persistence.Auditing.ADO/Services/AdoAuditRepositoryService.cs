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
 * Date: 2022-5-30
 */
using RestSrvr;
using SanteDB.Core.Model;
using SanteDB.Core;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Auditing.ADO.Configuration;
using SanteDB.Persistence.Auditing.ADO.Data.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using SanteDB.Core.Model.Query;
using System.Diagnostics.Tracing;
using SanteDB.BI.Services;
using SanteDB.BI.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.OrmLite.Migration;
using SanteDB.OrmLite.MappedResultSets;
using SanteDB.OrmLite.Providers;

namespace SanteDB.Persistence.Auditing.ADO.Services
{
    /// <summary>
    /// Represents a service which is responsible for the storage of audits
    /// </summary>
    /// TODO: Change this to wrapped call method
    [ServiceProvider("ADO.NET Audit Repository")]
    public class AdoAuditRepositoryService : IDataPersistenceService<AuditEventData>, IMappedQueryProvider<AuditEventData>
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Audit Repository";

        public IDbProvider Provider => throw new NotImplementedException();

        public IQueryPersistenceService QueryPersistence => throw new NotImplementedException();

        // Lock object
        private object m_lockBox = new object();

        // Configuration
        private readonly AdoAuditConfigurationSection m_configuration;

        // Data caching service
        private readonly IDataCachingService m_dataCachingService;

        // Concept repository
        private readonly IConceptRepositoryService m_conceptRepository;

        // Model map
        private ModelMapper m_mapper = null;

        // Query builder
        private QueryBuilder m_builder;

        private readonly IAdhocCacheService m_adhocCache;

        // Trace source name
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(AdoAuditRepositoryService));

#pragma warning disable CS0067
        /// <summary>
        /// Fired when data is being inserted
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<AuditEventData>> Inserting;

        /// <summary>
        /// Fired when data is has been inserted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<AuditEventData>> Inserted;

        /// <summary>
        /// Fired when data is being updated
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<AuditEventData>> Updating;

        /// <summary>
        /// Fired when data is has been inserted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<AuditEventData>> Updated;

        /// <summary>
        /// Fired when data is being obsoleted
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<AuditEventData>> Obsoleting
        {
            add { this.Deleting += value; }
            remove { this.Deleting -= value; }
        }

        /// <summary>
        /// Fired when data is has been inserted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<AuditEventData>> Obsoleted
        {
            add { this.Deleted += value; }
            remove { this.Deleted -= value; }
        }

        /// <summary>
        /// Fired when data is being retrieved
        /// </summary>
        public event EventHandler<DataRetrievingEventArgs<AuditEventData>> Retrieving;

        /// <summary>
        /// Fired when data is has been retrieved
        /// </summary>
        public event EventHandler<DataRetrievedEventArgs<AuditEventData>> Retrieved;

        /// <summary>
        /// Fired when data is being queryed
        /// </summary>
        public event EventHandler<QueryRequestEventArgs<AuditEventData>> Querying;

        /// <summary>
        /// Fired when data is has been queried
        /// </summary>
        public event EventHandler<QueryResultEventArgs<AuditEventData>> Queried;

        public event EventHandler<DataPersistedEventArgs<AuditEventData>> Deleted;

        public event EventHandler<DataPersistingEventArgs<AuditEventData>> Deleting;

#pragma warning restore CS0067

        /// <summary>
        /// Create new audit repository service
        /// </summary>
        public AdoAuditRepositoryService(IConfigurationManager configurationManager,
            IDataCachingService dataCachingService,
            IBiMetadataRepository biMetadataRepository,
            IConceptRepositoryService conceptRepository,
            IAdhocCacheService adhocCacheService = null)
        {
            this.m_configuration = configurationManager.GetSection<AdoAuditConfigurationSection>();
            this.m_adhocCache = adhocCacheService;
            this.m_dataCachingService = dataCachingService;
            this.m_conceptRepository = conceptRepository;

            try
            {
                this.m_configuration.Provider.UpgradeSchema("SanteDB.Persistence.Audit.ADO");

                ApplicationServiceContext.Current.Started += (o, e) =>
                {
                    using (AuthenticationContext.EnterSystemContext())
                    {
                        // Add audits as a BI data source
                        biMetadataRepository
                            .Insert(new BiDataSourceDefinition()
                            {
                                IsSystemObject = true,
                                ConnectionString = this.m_configuration.ReadonlyConnectionString,
                                MetaData = new BiMetadata()
                                {
                                    Version = typeof(AdoAuditRepositoryService).Assembly.GetName().Version.ToString(),
                                    Status = BiDefinitionStatus.Active,
                                    Demands = new List<string>()
                                    {
                                    PermissionPolicyIdentifiers.AccessAuditLog
                                    }
                                },
                                Id = "org.santedb.bi.dataSource.audit",
                                Name = "audit",
                                ProviderType = typeof(OrmBiDataProvider)
                            });
                    }
                };

                this.m_mapper = new ModelMapper(typeof(AdoAuditRepositoryService).Assembly.GetManifestResourceStream("SanteDB.Persistence.Auditing.ADO.Data.Map.ModelMap.xml"), AuditConstants.ModelMapName, true);
                this.m_builder = new QueryBuilder(this.m_mapper, this.m_configuration.Provider);
            }
            catch (ModelMapValidationException e)
            {
                this.m_traceSource.TraceError("Error validing map: {0}", e.Message);
                foreach (var i in e.ValidationDetails)
                    this.m_traceSource.TraceError("{0}:{1} @ {2}", i.Level, i.Message, i.Location);
                throw;
            }
        }

        /// <summary>
        /// Resolve code from central code repository
        /// </summary>
        private AuditCode ResolveCode(Guid key, String code, String codeSystem)
        {
            if (key == Guid.Empty) return null;

            var cacheItem = this.m_dataCachingService.GetCacheItem<Concept>(key);
            if (cacheItem == null)
            {
                if (!String.IsNullOrEmpty(codeSystem))
                    cacheItem = this.m_conceptRepository.GetConceptByReferenceTerm(code, codeSystem);
                if (cacheItem == null)
                    cacheItem = this.m_conceptRepository.GetConcept(code);
                if (cacheItem != null)
                    this.m_dataCachingService.Add(cacheItem);
            }
            return new AuditCode(code, codeSystem)
            {
                DisplayName = cacheItem?.ConceptNames?.FirstOrDefault()?.Name
            };
        }

        /// <summary>
        /// Convert a db audit to model
        /// </summary>
        private AuditEventData ToModelInstance(DataContext context, CompositeResult<DbAuditEventData, DbAuditCode> res, bool summary = true)
        {
            var retVal = this.m_dataCachingService.GetCacheItem<AuditEventData>(res.Object1.Key);
            if (retVal == null ||
                !summary)
            {
                retVal = new AuditEventData()
                {
                    ActionCode = (ActionType)res.Object1.ActionCode,
                    EventIdentifier = (EventIdentifierType)res.Object1.EventIdentifier,
                    Outcome = (OutcomeIndicator)res.Object1.Outcome,
                    Timestamp = res.Object1.Timestamp,
                    Key = res.Object1.Key
                };

                if (res.Object1.EventTypeCode != null)
                    retVal.EventTypeCode = this.ResolveCode(res.Object1.EventTypeCode, res.Object2.Code, res.Object2.CodeSystem);

                // Get actors and objects
                if (!summary)
                {
                    // Actors
                    var sql = context.CreateSqlStatement<DbAuditActorAssociation>().SelectFrom(typeof(DbAuditActorAssociation), typeof(DbAuditActor), typeof(DbAuditCode))
                            .InnerJoin<DbAuditActorAssociation, DbAuditActor>(o => o.TargetKey, o => o.Key)
                            .Join<DbAuditActor, DbAuditCode>("LEFT", o => o.ActorRoleCode, o => o.Key)
                            .Where<DbAuditActorAssociation>(o => o.SourceKey == res.Object1.Key)
                            .Build();

                    foreach (var itm in context.Query<CompositeResult<DbAuditActorAssociation, DbAuditActor, DbAuditCode>>(sql))
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserName = itm.Object2.UserName,
                            UserIsRequestor = itm.Object1.UserIsRequestor,
                            UserIdentifier = itm.Object2.UserIdentifier,
                            NetworkAccessPointId = itm.Object1.AccessPoint,
                            ActorRoleCode = new List<AuditCode>() { this.ResolveCode(itm.Object2.ActorRoleCode, itm.Object3.Code, itm.Object3.CodeSystem) }.OfType<AuditCode>().ToList()
                        });

                    // Objects
                    sql = context.CreateSqlStatement<DbAuditObject>().SelectFrom(typeof(DbAuditObject), typeof(DbAuditCode))
                        .Join<DbAuditObject, DbAuditCode>("LEFT", o => o.CustomIdType, o => o.Key)
                        .Where<DbAuditObject>(o => o.AuditId == res.Object1.Key);
                    foreach (var itm in context.Query<CompositeResult<DbAuditObject, DbAuditCode>>(sql).ToArray())
                    {
                        var ao = new AuditableObject()
                        {
                            IDTypeCode = (AuditableObjectIdType?)itm.Object1.IDTypeCode,
                            LifecycleType = (AuditableObjectLifecycle?)itm.Object1.LifecycleType,
                            NameData = itm.Object1.NameData,
                            ObjectId = itm.Object1.ObjectId,
                            QueryData = itm.Object1.QueryData,
                            Role = (AuditableObjectRole?)itm.Object1.Role,
                            Type = (AuditableObjectType)itm.Object1.Type
                        };

                        if (itm.Object1.CustomIdType.HasValue)
                        {
                            ao.CustomIdTypeCode = new AuditCode(itm.Object2.Code, itm.Object2.CodeSystem);
                        }

                        retVal.AuditableObjects.Add(ao);

                        foreach (var dat in context.Query<DbAuditObjectData>(o => o.ObjectId == itm.Object1.Key))
                        {
                            ao.ObjectData.Add(new ObjectDataExtension(dat.Name, dat.Value));
                        }
                    }

                    // Metadata

                    var stmt = context.CreateSqlStatement().SelectFrom(typeof(DbAuditMetadata))
                        .InnerJoin<DbAuditMetadata, DbAuditMetadataValue>(o => o.ValueId, o => o.Key)
                        .Where<DbAuditMetadata>(o => o.AuditId == res.Object1.Key);
                    foreach (var itm in context.Query<CompositeResult<DbAuditMetadata, DbAuditMetadataValue>>(stmt))
                    {
                        retVal.AddMetadata((AuditMetadataKey)itm.Object1.MetadataKey, itm.Object2.Value);
                    }
                }
                else
                {
                    // Actors
                    // Actors
                    var sql = context.CreateSqlStatement<DbAuditActorAssociation>().SelectFrom(typeof(DbAuditActorAssociation), typeof(DbAuditActor), typeof(DbAuditCode))
                            .InnerJoin<DbAuditActorAssociation, DbAuditActor>(o => o.TargetKey, o => o.Key)
                            .Join<DbAuditActor, DbAuditCode>("LEFT", o => o.ActorRoleCode, o => o.Key)
                            .Where<DbAuditActorAssociation>(o => o.SourceKey == res.Object1.Key)
                            .Build();

                    foreach (var itm in context.Query<CompositeResult<DbAuditActorAssociation, DbAuditActor, DbAuditCode>>(sql))
                        retVal.Actors.Add(new AuditActorData()
                        {
                            UserName = itm.Object2.UserName,
                            UserIsRequestor = itm.Object1.UserIsRequestor,
                            UserIdentifier = itm.Object2.UserIdentifier,
                            NetworkAccessPointId = itm.Object1.AccessPoint,
                            ActorRoleCode = new List<AuditCode>() { this.ResolveCode(itm.Object2.ActorRoleCode, itm.Object3.Code, itm.Object3.CodeSystem) }.OfType<AuditCode>().ToList()
                        });
                }

                this.m_dataCachingService.Add(retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Get or create audit code
        /// </summary>
        private DbAuditCode GetOrCreateAuditCode(DataContext context, AuditCode messageCode)
        {
            if (messageCode == null) return null;

            // Try to get from database
            lock (this.m_lockBox)
            {
                var retVal = context.FirstOrDefault<DbAuditCode>(o => o.Code == messageCode.Code && o.CodeSystem == messageCode.CodeSystem);
                if (retVal == null)
                {
                    Guid codeId = Guid.NewGuid();
                    retVal = context.Insert(new DbAuditCode() { Code = messageCode.Code, CodeSystem = messageCode.CodeSystem, Key = codeId });
                }
                return retVal;

            }
        }

        /// <summary>
        /// Insert the specified audit into the database
        /// </summary>
        public AuditEventData Insert(AuditEventData storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            // Pre-event trigger
            var preEvtData = new DataPersistingEventArgs<AuditEventData>(storageData, mode, overrideAuthContext);
            this.Inserting?.Invoke(this, preEvtData);
            if (preEvtData.Cancel)
            {
                this.m_traceSource.TraceEvent(EventLevel.Warning, "Pre-Event handler indicates abort insert {0}", storageData);
                return storageData;
            }

            // Insert
            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                IDbTransaction tx = null;
                try
                {
                    context.Open();
                    tx = context.BeginTransaction();

                    // Insert core
                    var dbAudit = this.m_mapper.MapModelInstance<AuditEventData, DbAuditEventData>(storageData);

                    var eventId = storageData.EventTypeCode;
                    if (eventId != null)
                        dbAudit.EventTypeCode = this.GetOrCreateAuditCode(context, eventId).Key;

                    dbAudit.CreationTime = DateTimeOffset.Now;
                    storageData.Key = Guid.NewGuid();
                    dbAudit.Key = storageData.Key.Value;
                    context.Insert(dbAudit);

                    // Insert secondary properties
                    if (storageData.Actors != null)
                        foreach (var act in storageData.Actors)
                        {
                            var roleCode = this.GetOrCreateAuditCode(context, act.ActorRoleCode.FirstOrDefault());

                            DbAuditActor dbAct = null;
                            if (roleCode != null)
                                dbAct = context.FirstOrDefault<DbAuditActor>(o => o.UserName == act.UserName && o.ActorRoleCode == roleCode.Key);
                            else
                                dbAct = context.FirstOrDefault<DbAuditActor>(o => o.UserName == act.UserName && o.ActorRoleCode == Guid.Empty);

                            if (dbAct == null)
                            {
                                dbAct = this.m_mapper.MapModelInstance<AuditActorData, DbAuditActor>(act);
                                dbAct.ActorRoleCode = roleCode?.Key ?? Guid.Empty;
                                dbAct = context.Insert(dbAct);
                            }
                            context.Insert(new DbAuditActorAssociation()
                            {
                                TargetKey = dbAct.Key,
                                SourceKey = dbAudit.Key,
                                UserIsRequestor = act.UserIsRequestor,
                                AccessPoint = act.NetworkAccessPointId
                            });
                        }

                    // Audit objects
                    if (storageData.AuditableObjects != null)
                        foreach (var ao in storageData.AuditableObjects)
                        {
                            var dbAo = this.m_mapper.MapModelInstance<AuditableObject, DbAuditObject>(ao);
                            dbAo.IDTypeCode = (int)(ao.IDTypeCode ?? 0);
                            dbAo.LifecycleType = (int)(ao.LifecycleType ?? 0);
                            dbAo.Role = (int)(ao.Role ?? 0);
                            dbAo.Type = (int)(ao.Type);
                            dbAo.AuditId = dbAudit.Key;

                            if (ao.CustomIdTypeCode != null)
                            {
                                var code = this.GetOrCreateAuditCode(context, ao.CustomIdTypeCode);
                                dbAo.CustomIdType = code.Key;
                            }

                            dbAo = context.Insert(dbAo);

                            if (ao.ObjectData?.Count > 0)
                            {
                                foreach (var od in ao.ObjectData)
                                {
                                    context.Insert(new DbAuditObjectData()
                                    {
                                        Name = od.Key,
                                        Value = od.Value,
                                        ObjectId = dbAo.Key
                                    });
                                }
                            }
                        }

                    // metadata
                    if (storageData.Metadata != null)
                        foreach (var meta in storageData.Metadata.Where(o => !String.IsNullOrEmpty(o.Value) && o.Key != AuditMetadataKey.CorrelationToken))
                        {
                            var kv = context.FirstOrDefault<DbAuditMetadataValue>(o => o.Value == meta.Value);
                            if (kv == null)
                                kv = context.Insert(new DbAuditMetadataValue()
                                {
                                    // TODO: Make this a common extension function (to trim)
                                    Value = meta.Value.Substring(0, meta.Value.Length > 256 ? 256 : meta.Value.Length)
                                });

                            context.Insert(new DbAuditMetadata()
                            {
                                AuditId = dbAudit.Key,
                                MetadataKey = (int)meta.Key,
                                ValueId = kv.Key
                            });
                        }

                    if (mode == TransactionMode.Commit)
                        tx.Commit();
                    else
                        tx.Rollback();

                    var args = new DataPersistedEventArgs<AuditEventData>(storageData, mode, overrideAuthContext);

                    this.Inserted?.Invoke(this, args);

                    return storageData;
                }
                catch (Exception ex)
                {
                    tx?.Rollback();
                    throw new Exception($"Error inserting audit {storageData.Key}", ex);
                }
            }
        }

        /// <summary>
        /// Update the audit - Not supported
        /// </summary>
        public AuditEventData Update(AuditEventData storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            throw new NotSupportedException("Updates not permitted");
        }

        /// <summary>
        /// Obsolete the audit - Not supported
        /// </summary>
        public AuditEventData Obsolete(Guid storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            throw new NotSupportedException("Obsoletion of audits not permitted");
        }

        /// <summary>
        /// Obsolete the audit - Not supported
        /// </summary>
        public AuditEventData Delete(Guid storageData, TransactionMode mode, IPrincipal overrideAuthContext)
        {
            throw new NotSupportedException("Delete of audits not permitted");
        }

        /// <summary>
        /// Gets the specified object by identifier
        /// </summary>
        public AuditEventData Get(Guid containerId, Guid? versionId, IPrincipal overrideAuthContext = null)
        {
            var preEvtData = new DataRetrievingEventArgs<AuditEventData>(containerId, versionId, overrideAuthContext);
            this.Retrieving?.Invoke(this, preEvtData);
            if (preEvtData.Cancel)
            {
                this.m_traceSource.TraceWarning("Pre-retrieval event indicates cancel {0}", containerId);
                return null;
            }

            try
            {
                var pk = containerId;

                // Fetch
                using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();

                    var sql = this.m_builder.CreateQuery<AuditEventData>(o => o.Key == pk).Build();
                    var res = context.FirstOrDefault<CompositeResult<DbAuditEventData, DbAuditCode>>(sql);
                    var result = this.ToModelInstance(context, res as CompositeResult<DbAuditEventData, DbAuditCode>, false);

                    var postEvtData = new DataRetrievedEventArgs<AuditEventData>(result, overrideAuthContext);
                    this.Retrieved?.Invoke(this, postEvtData);

                    return postEvtData.Data;
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error retrieving audit {0} : {1}", containerId, e);
                throw;
            }
        }

        /// <summary>
        /// Return a count of audits matching the query
        /// </summary>
        public long Count(Expression<Func<AuditEventData, bool>> query, IPrincipal overrideAuthContext = null)
        {
            var tr = 0;
            this.Query(query, 0, null, out tr, overrideAuthContext);
            return tr;
        }

        /// <summary>
        /// Execute a query
        /// </summary>
        public IQueryResultSet<AuditEventData> Query(Expression<Func<AuditEventData, bool>> query, IPrincipal overrideAuthContext = null) 
        {
            // TODO: Refactor this with a yield IQueryResultSet iterator
            var preEvtData = new QueryRequestEventArgs<AuditEventData>(query, principal: overrideAuthContext);
            this.Querying?.Invoke(this, preEvtData);
            if (preEvtData.Cancel)
            {
                this.m_traceSource.TraceWarning("Pre-event handler for query indicates cancel : {0}", query);
                return null;
            }

            try
            {
                using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();
                    var results = new MappedQueryResultSet<AuditEventData>(this).Where(query);

                    AuditUtil.AuditAuditLogUsed(ActionType.Read, OutcomeIndicator.Success, query.ToString(), results.Select(o => o.Key.Value).ToArray());

                    // Event args
                    var postEvtArgs = new QueryResultEventArgs<AuditEventData>(query, results, overrideAuthContext);
                    this.Queried?.Invoke(this, postEvtArgs);
                    return postEvtArgs.Results;
                }
            }
            catch (Exception e)
            {
                AuditUtil.AuditAuditLogUsed(ActionType.Read, OutcomeIndicator.EpicFail, query.ToString());
                this.m_traceSource.TraceError("Could not query audit {0}: {1}", query, e);
                throw;
            }
        }

        /// <summary>
        /// Executes a query for the specified objects
        /// </summary>
        public IEnumerable<AuditEventData> Query(Expression<Func<AuditEventData, bool>> query, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext = null, params ModelSort<AuditEventData>[] orderBy)
        {
            var result = this.Query(query, overrideAuthContext);
            totalCount = result.Count();
            return result.Skip(offset).Take(count ?? 100);
        }

        /// <inheritdoc/>
        public IOrmResultSet ExecuteQueryOrm(DataContext context, Expression<Func<AuditEventData, bool>> query)
        {
            var sql = this.m_builder.CreateQuery(query).Build();
            return context.Query<CompositeResult<DbAuditEventData, DbAuditCode>>(sql);
        }

        /// <summary>
        /// Get the specified audit
        /// </summary>
        public AuditEventData Get(DataContext context, Guid key)
        {
            var auditData = context.FirstOrDefault<DbAuditEventData>(o => o.Key == key);
            return this.ToModelInstance(context, auditData);
        }

        /// <summary>
        /// Map to model instance
        /// </summary>
        public AuditEventData ToModelInstance(DataContext context, object result)
        {
            switch(result)
            {
                case CompositeResult<DbAuditEventData, DbAuditCode> cr:
                    return this.ToModelInstance(context, cr);
                case CompositeResult cr2:
                    return this.ToModelInstance(context, new CompositeResult<DbAuditEventData, DbAuditCode>(cr2.Values.OfType<DbAuditEventData>().First(), cr2.Values.OfType<DbAuditCode>().First()));
                case DbAuditEventData ae:
                    var other = context.FirstOrDefault<DbAuditCode>(o => o.Key == ae.EventTypeCode);
                    return this.ToModelInstance(context, new CompositeResult<DbAuditEventData, DbAuditCode>(ae, other));
                default:
                    throw new InvalidOperationException(SanteDB.Core.i18n.ErrorMessages.MAP_INCOMPATIBLE_TYPE);

            }
        }

        /// <summary>
        /// Map to a database sort expression
        /// </summary>
        public Expression MapExpression<TReturn>(Expression<Func<AuditEventData, TReturn>> sortExpression)
        {
            return this.m_mapper.MapModelExpression<AuditEventData, DbAuditEventData, TReturn>(sortExpression);
        }

#pragma warning restore CS0067
    }
}