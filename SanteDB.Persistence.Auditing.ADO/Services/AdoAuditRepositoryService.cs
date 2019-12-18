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
 * User: justin
 * Date: 2018-6-22
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Auditing;
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

namespace SanteDB.Persistence.Auditing.ADO.Services
{
    /// <summary>
    /// Represents a service which is responsible for the storage of audits
    /// </summary>
    [ServiceProvider("ADO.NET Audit Repository")]
#pragma warning disable CS0067
    public class AdoAuditRepositoryService : IDataPersistenceService<AuditData>
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Audit Repository";

        // Confiugration
        private AdoAuditConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoAuditConfigurationSection>();

        // Model map
        private ModelMapper m_mapper = null;

        // Query builder
        private QueryBuilder m_builder;

        // Trace source name
        private Tracer m_traceSource = new Tracer(AuditConstants.TraceSourceName);

        /// <summary>
        /// Fired when data is being inserted
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<AuditData>> Inserting;

        /// <summary>
        /// Fired when data is has been inserted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<AuditData>> Inserted;
        /// <summary>
        /// Fired when data is being updated
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<AuditData>> Updating;
        /// <summary>
        /// Fired when data is has been inserted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<AuditData>> Updated;
        /// <summary>
        /// Fired when data is being obsoleted
        /// </summary>
        public event EventHandler<DataPersistingEventArgs<AuditData>> Obsoleting;
        /// <summary>
        /// Fired when data is has been inserted
        /// </summary>
        public event EventHandler<DataPersistedEventArgs<AuditData>> Obsoleted;
        /// <summary>
        /// Fired when data is being retrieved
        /// </summary>
        public event EventHandler<DataRetrievingEventArgs<AuditData>> Retrieving;
        /// <summary>
        /// Fired when data is has been retrieved
        /// </summary>
        public event EventHandler<DataRetrievedEventArgs<AuditData>> Retrieved;
        /// <summary>
        /// Fired when data is being queryed
        /// </summary>
        public event EventHandler<QueryRequestEventArgs<AuditData>> Querying;
        /// <summary>
        /// Fired when data is has been queried
        /// </summary>
        public event EventHandler<QueryResultEventArgs<AuditData>> Queried;

        /// <summary>
        /// Create new audit repository service
        /// </summary>
        public AdoAuditRepositoryService()
        {
            try
            {
                ApplicationServiceContext.Current.Started += (o, e) =>
                {

                    // Add audits as a BI data source
                    ApplicationServiceContext.Current.GetService<IBiMetadataRepository>()
                        .Insert(new BiDataSourceDefinition()
                        {
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
                };

                this.m_mapper = new ModelMapper(typeof(AdoAuditRepositoryService).Assembly.GetManifestResourceStream("SanteDB.Persistence.Auditing.ADO.Data.Map.ModelMap.xml"));
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
        /// Resolve code
        /// </summary>
        private AuditCode ResolveCode(Guid key, String code, String codeSystem)
        {
            if (key == Guid.Empty) return null;
            var cache = ApplicationServiceContext.Current.GetService<IDataCachingService>();

            var cacheItem = cache.GetCacheItem<Concept>(key);
            if (cacheItem == null)
            {
                if(!String.IsNullOrEmpty(codeSystem))
                    cacheItem = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(code, codeSystem).FirstOrDefault();
                if (cacheItem == null)
                    cacheItem = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConcept(code);
                if (cacheItem != null)
                    cache.Add(cacheItem);
            }
            return new AuditCode(code, codeSystem)
            {
                DisplayName = cacheItem?.ConceptNames?.FirstOrDefault()?.Name
            };
        }

        /// <summary>
        /// Convert a db audit to model 
        /// </summary>
        private AuditData ToModelInstance(DataContext context, CompositeResult<DbAuditData, DbAuditCode> res, bool summary = true)
        {
            var retVal = ApplicationServiceContext.Current.GetService<IDataCachingService>()?.GetCacheItem<AuditData>(res.Object1.Key);
            if (retVal == null ||
                !summary && retVal.LoadState < Core.Model.LoadState.FullLoad)
            {

                retVal = new AuditData()
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
                    foreach (var itm in context.Query<DbAuditObject>(o => o.AuditId == res.Object1.Key))
                    {
                        retVal.AuditableObjects.Add(new AuditableObject()
                        {
                            IDTypeCode = (AuditableObjectIdType?)itm.IDTypeCode,
                            LifecycleType = (AuditableObjectLifecycle?)itm.LifecycleType,
                            NameData = itm.NameData,
                            ObjectId = itm.ObjectId,
                            QueryData = itm.QueryData,
                            Role = (AuditableObjectRole?)itm.Role,
                            Type = (AuditableObjectType)itm.Type
                        });
                    }

                    // Metadata
                    foreach (var itm in context.Query<DbAuditMetadata>(o => o.AuditId == res.Object1.Key))
                        retVal.AddMetadata((AuditMetadataKey)itm.MetadataKey, itm.Value);

                    retVal.LoadState = Core.Model.LoadState.FullLoad;
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

                    retVal.LoadState = Core.Model.LoadState.PartialLoad;

                }

                ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Add(retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Insert the specified audit into the database
        /// </summary>
        public AuditData Insert(AuditData storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {

            // Pre-event trigger
            var preEvtData = new DataPersistingEventArgs<AuditData>(storageData, overrideAuthContext);
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
                    var dbAudit = this.m_mapper.MapModelInstance<AuditData, DbAuditData>(storageData);

                    var eventId = storageData.EventTypeCode;
                    if (eventId != null)
                    {
                        var existing = context.FirstOrDefault<DbAuditCode>(o => o.Code == eventId.Code && o.CodeSystem == eventId.CodeSystem);
                        if (existing == null)
                        {
                            Guid codeId = Guid.NewGuid();
                            dbAudit.EventTypeCode = codeId;
                            context.Insert(new DbAuditCode() { Code = eventId.Code, CodeSystem = eventId.CodeSystem, Key = codeId });
                        }
                        else
                            dbAudit.EventTypeCode = existing.Key;
                    }

                    dbAudit.CreationTime = DateTime.Now;
                    storageData.Key = Guid.NewGuid();
                    dbAudit.Key = storageData.Key.Value;
                    context.Insert(dbAudit);

                    // Insert secondary properties
                    if (storageData.Actors != null)
                        foreach (var act in storageData.Actors)
                        {
                            var dbAct = context.FirstOrDefault<DbAuditActor>(o => o.UserName == act.UserName);
                            if (dbAct == null)
                            {
                                dbAct = this.m_mapper.MapModelInstance<AuditActorData, DbAuditActor>(act);
                                dbAct.Key = Guid.NewGuid();
                                var roleCode = act.ActorRoleCode?.FirstOrDefault();
                                if (roleCode != null)
                                {
                                    var existing = context.FirstOrDefault<DbAuditCode>(o => o.Code == roleCode.Code && o.CodeSystem == roleCode.CodeSystem);
                                    if (existing == null)
                                    {
                                        dbAct.ActorRoleCode = Guid.NewGuid();
                                        context.Insert(new DbAuditCode() { Code = roleCode.Code, CodeSystem = roleCode.CodeSystem, Key = dbAct.ActorRoleCode });
                                    }
                                    else
                                        dbAct.ActorRoleCode = existing.Key;
                                }
                                context.Insert(dbAct);

                            }
                            context.Insert(new DbAuditActorAssociation()
                            {
                                TargetKey = dbAct.Key,
                                SourceKey = dbAudit.Key,
                                UserIsRequestor = act.UserIsRequestor, 
                                AccessPoint = act.NetworkAccessPointId,
                                Key = Guid.NewGuid()
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
                            dbAo.Key = Guid.NewGuid();
                            context.Insert(dbAo);
                        }

                    // metadata
                    if(storageData.Metadata != null)
                        foreach(var meta in storageData.Metadata)
                            context.Insert(new DbAuditMetadata()
                            {
                                AuditId = dbAudit.Key,
                                MetadataKey = (int)meta.Key,
                                Value = meta.Value
                            });

                    if (mode == TransactionMode.Commit)
                        tx.Commit();
                    else
                        tx.Rollback();

                    var args = new DataPersistedEventArgs<AuditData>(storageData, overrideAuthContext);

                    this.Inserted?.Invoke(this, args);

                    return storageData;
                }
                catch (Exception ex)
                {
                    tx?.Rollback();
                    this.m_traceSource.TraceError("Error inserting audit: {0}", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Update the audit - Not supported
        /// </summary>
        public AuditData Update(AuditData storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            throw new NotSupportedException("Updates not permitted");
        }

        /// <summary>
        /// Obsolete the audit - Not supported
        /// </summary>
        public AuditData Obsolete(AuditData storageData, TransactionMode mode, IPrincipal overrideAuthContext = null)
        {
            throw new NotSupportedException("Obsoletion of audits not permitted");
        }

        /// <summary>
        /// Gets the specified object by identifier
        /// </summary>
        public AuditData Get(Guid containerId, Guid? versionId, bool loadFast = false, IPrincipal overrideAuthContext = null)
        {

            var preEvtData = new DataRetrievingEventArgs<AuditData>(containerId, versionId, overrideAuthContext);
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

                    var sql = this.m_builder.CreateQuery<AuditData>(o => o.Key == pk).Build();
                    var res = context.FirstOrDefault<CompositeResult<DbAuditData, DbAuditCode>>(sql);
                    var result = this.ToModelInstance(context, res as CompositeResult<DbAuditData, DbAuditCode>, false);

                    var postEvtData = new DataRetrievedEventArgs<AuditData>(result, overrideAuthContext);
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
        public long Count(Expression<Func<AuditData, bool>> query, IPrincipal overrideAuthContext = null)
        {
            var tr = 0;
            this.Query(query, 0, null, out tr, overrideAuthContext);
            return tr;
        }

        /// <summary>
        /// Execute a query
        /// </summary>
        public IEnumerable<AuditData> Query(Expression<Func<AuditData, bool>> query, IPrincipal overrideAuthContext = null)
        {
            int tr = 0;
            return this.Query(query, 0, null, out tr);
        }

        /// <summary>
        /// Executes a query for the specified objects
        /// </summary>
        public IEnumerable<AuditData> Query(Expression<Func<AuditData, bool>> query, int offset, int? count, out int totalCount, IPrincipal overrideAuthContext = null, params ModelSort<AuditData>[] orderBy)
        {

            var preEvtData = new QueryRequestEventArgs<AuditData>(query, offset: offset, count: count, queryId: null, principal: overrideAuthContext);
            this.Querying?.Invoke(this, preEvtData);
            if (preEvtData.Cancel)
            {
                this.m_traceSource.TraceWarning("Pre-event handler for query indicates cancel : {0}", query);
                totalCount = 0;
                return null;
            }

            try
            {
                using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();

                    var sql = this.m_builder.CreateQuery(query).Build();

                    if (orderBy != null && orderBy.Length > 0)
                        foreach (var ob in orderBy)
                            sql = sql.OrderBy<DbAuditData>(this.m_mapper.MapModelExpression<AuditData, DbAuditData, dynamic>(ob.SortProperty), ob.SortOrder);
                    else
                        sql = sql.OrderBy<DbAuditData>(o => o.Timestamp, SortOrderType.OrderByDescending);

                    // Total results
                    totalCount = context.Count(sql);

                    // Query control
                    if (count.GetValueOrDefault() == 0)
                        sql.Offset(offset).Limit(100);
                    else
                        sql.Offset(offset).Limit(count.Value);
                    sql = sql.Build();
                    var itm = context.Query<CompositeResult<DbAuditData, DbAuditCode>>(sql).ToList();
                    AuditUtil.AuditAuditLogUsed(ActionType.Read, OutcomeIndicator.Success, sql.ToString(), itm.Select(o => o.Object1.Key).ToArray());
                    var results = itm.Select(o => this.ToModelInstance(context, o)).ToList().AsQueryable();

                    // Event args
                    var postEvtArgs = new QueryResultEventArgs<AuditData>(query, results, offset, count, totalCount, null, overrideAuthContext);
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
    }
#pragma warning restore CS0067

}
