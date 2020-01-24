/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Services;
using SanteDB.Persistence.MDM.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

using System.Security.Permissions;
using System.Security.Principal;

namespace SanteDB.Persistence.MDM.Services
{

    /// <summary>
    /// Abstract wrapper for MDM resource listeners
    /// </summary>
    public abstract class MdmResourceListener : IDisposable
    {
        /// <summary>
        /// Dispose
        /// </summary>
        public abstract void Dispose();
    }

    /// <summary>
    /// Represents a base class for an MDM resource listener
    /// </summary>
    public class MdmResourceListener<T> : MdmResourceListener, IRecordMergingService<T>
        where T : IdentifiedData, new()
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => $"MIDM Data Handler Listener for {typeof(T).FullName}";

        // Configuration
        private ResourceMergeConfiguration m_resourceConfiguration;

        // Tracer
        private Tracer m_traceSource = new Tracer(MdmConstants.TraceSourceName);

        // The repository that this listener is attached to
        private INotifyRepositoryService<T> m_repository;

        // Persistence service
        private IDataPersistenceService<Bundle> m_persistence;

        /// <summary>
        /// Fired when the service is merging
        /// </summary>
        public event EventHandler<DataMergingEventArgs<T>> Merging;

        /// <summary>
        /// Fired when data has been merged
        /// </summary>
        public event EventHandler<DataMergeEventArgs<T>> Merged;

        /// <summary>
        /// Resource listener
        /// </summary>
        public MdmResourceListener(ResourceMergeConfiguration configuration)
        {
            // Register the master 
            ModelSerializationBinder.RegisterModelType($"{typeof(T).Name}Master", typeof(Entity).IsAssignableFrom(typeof(T)) ? typeof(EntityMaster<T>) : typeof(ActMaster<T>));
            this.m_resourceConfiguration = configuration;
            this.m_persistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>();
            if (this.m_persistence == null)
                throw new InvalidOperationException($"Could not find persistence service for Bundle");

            this.m_repository = ApplicationServiceContext.Current.GetService<IRepositoryService<T>>() as INotifyRepositoryService<T>;
            if (this.m_repository == null)
                throw new InvalidOperationException($"Could not find repository service for {typeof(T)}");
            // Subscribe
            this.m_repository.Inserting += this.OnPrePersistenceValidate;
            this.m_repository.Saving += this.OnPrePersistenceValidate;
            this.m_repository.Inserting += this.OnInserting;
            this.m_repository.Saving += this.OnSaving;
            this.m_repository.Obsoleting += this.OnObsoleting;
            this.m_repository.Retrieved += this.OnRetrieved;
            this.m_repository.Retrieving += this.OnRetrieving;
            this.m_repository.Queried += this.OnQueried;
            this.m_repository.Querying += this.OnQuerying;
        }

        /// <summary>
        /// Handles before a subscribe object is queried
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks><para>Unless a local tag is specified, this command will ensure that only MASTER records are returned
        /// to the client, otherwise it will ensure only LOCAL records are returned.</para>
        /// <para>This behavior ensures that clients interested in LOCAL records only get only their locally contributed records
        /// otherwise they will receive the master records.
        /// </para>
        /// </remarks>
        protected virtual void OnQuerying(object sender, QueryRequestEventArgs<T> e)
        {
            var query = new NameValueCollection(QueryExpressionBuilder.BuildQuery<T>(e.Query).ToArray());

            // The query doesn't contain a query for master records, so...
            // If the user is not in the role "SYSTEM" OR they didn't ask specifically for LOCAL records we have to rewrite the query to use MASTER
            if (!e.Principal.IsInRole("SYSTEM") || !query.ContainsKey("tag[mdm.type].value"))
            {
                // Did the person ask specifically for a local record? if so we need to demand permission
                if (query.TryGetValue("tag[mdm.type].value", out List<String> mdmFilters) && mdmFilters.Contains("L"))
                    new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.ReadMdmLocals).Demand();
                else // We want to modify the query to only include masters and rewrite the query
                {
                    var localQuery = new NameValueCollection(query.ToDictionary(o => $"relationship[MDM-Master].source@{typeof(T).Name}.{o.Key}", o => o.Value));
                    localQuery.Add("classConcept", MdmConstants.MasterRecordClassification.ToString());
                    query.Add("classConcept", MdmConstants.MasterRecordClassification.ToString());
                    e.Cancel = true; // We want to cancel the other's query

                    // We are wrapping an entity, so we query entity masters
                    int tr = 0;
                    if (typeof(Entity).IsAssignableFrom(typeof(T)))
                        e.Results = this.MasterQuery<Entity>(query, localQuery, e.QueryId.GetValueOrDefault(), e.Offset, e.Count, e.Principal, out tr);
                    else
                        e.Results = this.MasterQuery<Act>(query, localQuery, e.QueryId.GetValueOrDefault(), e.Offset, e.Count, e.Principal, out tr);
                    e.TotalResults = tr;
                }
            }
        }

        /// <summary>
        /// Perform a master query 
        /// </summary>
        /// <param name="count">The number of results to return</param>
        /// <param name="localQuery">The query for local records affixed to the MDM tree</param>
        /// <param name="masterQuery">The query for master records</param>
        /// <param name="offset">The offset of the first result</param>
        /// <param name="principal">The user executing the query</param>
        /// <param name="queryId">The unique query identifier</param>
        ///<param name="totalResults">The number of matching results</param>
        private IEnumerable<T> MasterQuery<TMasterType>(NameValueCollection masterQuery, NameValueCollection localQuery, Guid queryId, int offset, int? count, IPrincipal principal, out int totalResults)
            where TMasterType : IdentifiedData
        {
            var qpi = ApplicationServiceContext.Current.GetService<IStoredQueryDataPersistenceService<TMasterType>>();
            IEnumerable<TMasterType> results = null;
            if (qpi is IUnionQueryDataPersistenceService<TMasterType> iqps)
            {
                // Try to do a linked query (unless the query is on a special local filter value)
                try
                {
                    var masterLinq = QueryExpressionParser.BuildLinqExpression<TMasterType>(masterQuery, null, false);
                    var localLinq = QueryExpressionParser.BuildLinqExpression<TMasterType>(localQuery, null, false);
                    results = iqps.Union(new Expression<Func<TMasterType, bool>>[] { masterLinq, localLinq }, queryId, offset, count, out totalResults, principal);
                }
                catch
                {
                    var localLinq = QueryExpressionParser.BuildLinqExpression<TMasterType>(localQuery, null, false);
                    results = qpi.Query(localLinq, queryId, offset, count, out totalResults, principal);
                }
            }
            else
            { // Not capable of doing intersect results at query level
                var masterLinq = QueryExpressionParser.BuildLinqExpression<TMasterType>(localQuery, null, false);
                results = qpi.Query(masterLinq, queryId, offset, count, out totalResults, principal);
            }
            return results.AsParallel().AsOrdered().Select(o => o is Entity ? new EntityMaster<T>((Entity)(object)o).GetMaster(principal) : new ActMaster<T>((Act)(Object)o).GetMaster(principal)).OfType<T>().ToList();
        }

        /// <summary>
        /// Handles when a subscribed object is queried
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>The MDM provider will ensure that no data from the LOCAL instance which is masked is returned
        /// in the MASTER record</remarks>
        protected virtual void OnQueried(object sender, QueryResultEventArgs<T> e)
        {
            // TODO: Filter master record data based on taboo child records.

        }

        /// <summary>
        /// Handles when subscribed object is being retrieved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>The MDM records are actually redirected types. For example, a request to retrieve a Patient 
        /// which is a master is actually retrieving an entity which has a synthetic record of type Patient. If we 
        /// don't redirect these requests then technically a request to retrieve a master will result in an emtpy / exception
        /// case.</remarks>
        protected virtual void OnRetrieving(object sender, DataRetrievingEventArgs<T> e)
        {
            // There aren't actually any data in the database which is of this type
            ApplicationServiceContext.Current.GetService<IDataPersistenceService<T>>().Query(o => o.Key == e.Id, 0, 0, out int records, AuthenticationContext.SystemPrincipal);
            if (records == 0) //
            {
                e.Cancel = true;
                if (typeof(Entity).IsAssignableFrom(typeof(T)))
                {
                    var master = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Entity>>().Get(e.Id.Value, null, false, AuthenticationContext.Current.Principal);
                    e.Result = new EntityMaster<T>(master).GetMaster(AuthenticationContext.Current.Principal);
                }
                else if (typeof(Act).IsAssignableFrom(typeof(T)))
                {
                    var master = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Act>>().Get(e.Id.Value, null, false, AuthenticationContext.Current.Principal);
                    e.Result = new ActMaster<T>(master).GetMaster(AuthenticationContext.Current.Principal);
                }
            }
        }

        /// <summary>
        /// Handles when subscribed object is retrieved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>MDM provider will ensure that if the retrieved record is a MASTER record, that no 
        /// data from masked LOCAL records is included.</remarks>
        protected virtual void OnRetrieved(object sender, DataRetrievedEventArgs<T> e)
        {
            // We have retrieved an object from the database. If it is local we have to ensure that 
            // 1. The user actually requested the local
            // 2. The user is the original owner of the local, or
            // 2a. The user has READ LOCAL permission
            if ((e.Data as ITaggable)?.Tags.Any(t => t.TagKey == "mdm.type" && t.Value == "L") == true) // is a local record
            {
                // Is the requesting user the provenance of that record?
                this.EnsureProvenance(e.Data, AuthenticationContext.Current.Principal);
            }

        }
        
        /// <summary>
        /// Validates that a MASTER record is only being inserted by this class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>We don't want clients submitting MASTER records, so this method will ensure that all records
        /// being sent with tag of MASTER are indeed sent by the MDM or SYSTEM user.</remarks>
        protected virtual void OnPrePersistenceValidate(object sender, DataPersistingEventArgs<T> e)
        {
            Guid? classConcept = (e.Data as Entity)?.ClassConceptKey ?? (e.Data as Act)?.ClassConceptKey;
            // We are touching a master record and we are not system?
            if (classConcept.GetValueOrDefault() == MdmConstants.MasterRecordClassification &&
                !e.Principal.IsInRole("SYSTEM"))
                new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.WriteMdmMaster).Demand();

            // We want to ensure that a master link is not being explicitly persisted
            var identified = e.Data as IIdentifiedEntity;
            int tr = 0;
            if (e.Data is Entity)
            {
                var eRelationship = (e.Data as Entity).LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.CandidateLocalRelationship);
                if (eRelationship != null)
                {
                    // Get existing er if available
                    var dbRelationship = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Query(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.CandidateLocalRelationship && o.SourceEntityKey == identified.Key, 0, 1, out tr, e.Principal);
                    if (tr == 0 || dbRelationship.First().TargetEntityKey == eRelationship.TargetEntityKey)
                        return;
                    else if (!e.Principal.IsInRole("SYSTEM")) // The target entity is being re-associated make sure the principal is allowed to do this
                        new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.WriteMdmMaster).Demand();
                }

                if ((e.Data as ITaggable)?.Tags.Any(o => o.TagKey == "mdm.type" && o.Value == "T") == true &&
                        (e.Data as Entity).Relationships.Single(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship) == null)
                    throw new InvalidOperationException("Records of truth must have exactly one MASTER");

            }
            else if (e.Data is Act)
            {
                var eRelationship = (e.Data as Act).LoadCollection<ActRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.CandidateLocalRelationship);
                if (eRelationship != null)
                {
                    // Get existing er if available
                    var dbRelationship = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActRelationship>>().Query(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.CandidateLocalRelationship && o.SourceEntityKey == identified.Key, 0, 1, out tr, e.Principal);
                    if (tr == 0 || dbRelationship.First().TargetActKey == eRelationship.TargetActKey)
                        return;
                    else if (!e.Principal.IsInRole("SYSTEM")) // The target entity is being re-associated make sure the principal is allowed to do this
                        new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.WriteMdmMaster).Demand();
                }

                if ((e.Data as ITaggable)?.Tags.Any(o => o.TagKey == "mdm.type" && o.Value == "T") == true &&
                    (e.Data as Act).Relationships.Single(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship) == null)
                    throw new InvalidOperationException("Records of truth must have exactly one MASTER");

            }

        }

        /// <summary>
        /// Fired before a record is obsoleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>We don't want a MASTER record to be obsoleted under any condition. MASTER records require special permission to 
        /// obsolete and also require that all LOCAL records be either re-assigned or obsoleted as well.</remarks>
        protected virtual void OnObsoleting(object sender, DataPersistingEventArgs<T> e)
        {
            // Obsoleting a master record requires that the user be a SYSTEM user or has WriteMDM permission
            Guid? classConcept = (e.Data as Entity)?.ClassConceptKey ?? (e.Data as Act)?.ClassConceptKey;
            // We are touching a master record and we are not system?
            if (classConcept.GetValueOrDefault() == MdmConstants.MasterRecordClassification &&
                !e.Principal.IsInRole("SYSTEM"))
                new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.WriteMdmMaster).Demand();

            // We will receive an obsolete on a MASTER for its type however the repository needs to be redirected as we aren't getting that particular object
        }

        /// <summary>
        /// Called when a record is being created or updated
        /// </summary>
        protected virtual void OnSaving(object sender, DataPersistingEventArgs<T> e)
        {
            // Is this object a ROT or MASTER, if it is then we do not perform any changes to re-binding
            var taggable = e.Data as ITaggable;
           
            var mdmTag = taggable?.Tags.FirstOrDefault(o => o.TagKey == "mdm.type");
            if (mdmTag?.Value == "T")
                return; // Record of truth is never re-matched and remains bound to the original object
            else if (mdmTag == null || mdmTag?.Value != "M") // record is a local and may need to be re-matched
            {
                if (e.Data is Entity && mdmTag == null)
                {
                    var et = new EntityTag("mdm.type", "L");
                    (e.Data as Entity).Tags.Add(et);
                    mdmTag = et;
                }
                else if (e.Data is Act && mdmTag == null)
                {
                    var at = new ActTag("mdm.type", "L");
                    (e.Data as Act).Tags.Add(at);
                    mdmTag = at;
                }

                // Perform matching
                var bundle = this.PerformMdmMatch(e.Data);
                e.Cancel = true;

                // Is the caller the bundle MDM? if so just add 
                if (sender is Bundle)
                {
                    (sender as Bundle).Item.Remove(e.Data);
                    (sender as Bundle).Item.AddRange(bundle.Item);
                }
                else
                {
                    // Manually fire the business rules trigger for Bundle
                    var businessRulesService = ApplicationServiceContext.Current.GetBusinessRulesService<Bundle>();
                    bundle = businessRulesService?.BeforeUpdate(bundle) ?? bundle;
                    // Business rules shouldn't be used for relationships, we need to delay load the sources
                    bundle.Item.OfType<EntityRelationship>().ToList().ForEach((i) =>
                    {
                        if (i.SourceEntity == null)
                            i.SourceEntity = bundle.Item.Find(o => o.Key == i.SourceEntityKey) as Entity;
                    });
                    bundle = this.m_persistence.Update(bundle, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                    bundle = businessRulesService?.AfterUpdate(bundle) ?? bundle;
                }
                //ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem(this.PerformMdmMatch, identified);
            }

        }
        
        /// <summary>
        /// Fired after record is inserted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>This method will fire the record matching service and will ensure that duplicates are marked
        /// and merged into any existing MASTER record.</remarks>
        protected virtual void OnInserting(object sender, DataPersistingEventArgs<T> e)
        {

            if (!e.Data.Key.HasValue)
                e.Data.Key = Guid.NewGuid(); // Assign a key if one is not set

            // Gather tags to determine whether the object has been linked to a master
            var taggable = e.Data as ITaggable;
            var mdmTag = taggable?.Tags.FirstOrDefault(o => o.TagKey == "mdm.type");
            if (mdmTag == null || (mdmTag.Value != "M" && mdmTag.Value != "T")) // Record is a master - MASTER duplication on input is rare
            {
                if (e.Data is Entity)
                {
                    var et = new EntityTag("mdm.type", "L");
                    (e.Data as Entity).Tags.Add(et);
                    mdmTag = et;
                }
                else if (e.Data is Act)
                {
                    var at = new ActTag("mdm.type", "L");
                    (e.Data as Act).Tags.Add(at);
                    mdmTag = at;
                }
            }

            // Find records for which this record could be match // async
            if (mdmTag?.Value == "L")
            {
                e.Cancel = true;
                var bundle = this.PerformMdmMatch(e.Data);

                // Is the caller the bundle MDM? if so just add 
                if (sender is Bundle)
                {
                    //(sender as Bundle).Item.Remove(e.Data);
                    (sender as Bundle).Item.AddRange(bundle.Item.Where(o=> o != e.Data));
                }
                else
                {
                    var businessRulesSerice = ApplicationServiceContext.Current.GetBusinessRulesService<Bundle>();
                    bundle = businessRulesSerice?.BeforeInsert(bundle) ?? bundle;
                    // Business rules shouldn't be used for relationships, we need to delay load the sources
                    bundle.Item.OfType<EntityRelationship>().ToList().ForEach((i) =>
                    {
                        if (i.SourceEntity == null)
                            i.SourceEntity = bundle.Item.Find(o => o.Key == i.SourceEntityKey) as Entity;
                    });
                    bundle = this.m_persistence.Insert(bundle, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                    bundle = businessRulesSerice?.AfterInsert(bundle) ?? bundle;
                }
                //ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem(this.PerformMdmMatch, identified);
            }

        }

        /// <summary>
        /// Perform an MDM match process to link the probable and definitive match
        /// </summary>
        private Bundle PerformMdmMatch(T entity)
        {
            var matchService = ApplicationServiceContext.Current.GetService<IRecordMatchingService>();
            if (matchService == null)
                throw new InvalidOperationException("Cannot operate MDM mode without matching service"); // Cannot make determination of matching

            var taggable = (ITaggable)entity;
            var relationshipType = entity is Entity ? typeof(EntityRelationship) : typeof(ActRelationship);
            var relationshipService = ApplicationServiceContext.Current.GetService(typeof(IDataPersistenceService<>).MakeGenericType(relationshipType)) as IDataPersistenceService;

            // Create generic method for call with proper arguments
            var matchMethod = typeof(IRecordMatchingService).GetGenericMethod(nameof(IRecordMatchingService.Match), new Type[] { entity.GetType() }, new Type[] { entity.GetType(), typeof(String) });
            if (matchMethod == null)
                throw new InvalidOperationException("State is invalid - Could not find matching service method - Does it implement IRecordMatchingService properly?");

            var rawMatches = matchMethod.Invoke(matchService, new object[] { entity, this.m_resourceConfiguration.MatchConfiguration }) as IEnumerable;
            var matchingRecords = rawMatches.OfType<IRecordMatchResult>();
            // Matching records can only match with those that have MASTER records
            var matchGroups = matchingRecords
                .Where(o=>o.Record.Key != entity.Key)
                .Select(o => new MasterMatch(this.GetMaster(o.Record).Value, o))
                .Distinct(new MasterMatchEqualityComparer())
                .GroupBy(o => o.MatchResult.Classification)
                .ToDictionary(o => o.Key, o => o.Select(g => g.Master).Distinct());
                
            if (!matchGroups.ContainsKey(RecordMatchClassification.Match))
                matchGroups.Add(RecordMatchClassification.Match, null);
            if (!matchGroups.ContainsKey(RecordMatchClassification.Probable))
                matchGroups.Add(RecordMatchClassification.Probable, null);

            // Record is a LOCAL record
            //// INPUT = INBOUND LOCAL RECORD (FROM PATIENT SOURCE) THAT HAS BEEN INSERTED 
            //// MATCHES = THE RECORDS THAT HAVE BEEN DETERMINED TO BE DEFINITE MATCHES WHEN COMPARED TO INPUT 
            //// PROBABLES = THE RECORDS THAT HAVE BEEN DETERMINED TO BE POTENTIAL MATCHES WHEN COMPARE TO INPUT
            //// AUTOMERGE = A CONFIGURATION VALUE WHICH INSTRUCTS THE MPI TO AUTOMATICALLY MERGE DATA WHEN SAFE

            //// THE MATCH SERVICE HAS FOUND 1 DEFINITE MASTER RECORD THAT MATCHES THE INPUT RECORD AND AUTOMERGE IS ON
            //IF MATCHES.COUNT = 1 AND AUTOMERGE = TRUE THEN
            //        INPUT.MASTER = MATCHES[0]; // ASSIGN THE FOUND MATCH AS THE MASTER RECORD OF THE INPUT
            //// THE MATCH SERVICE HAS FOUND NO DEFINITE MASTER RECORDS THAT MATCH THE INPUT RECORD OR 1 WAS FOUND AND AUTOMERGE IS OFF
            //ELSE
            //        INPUT.MASTER = NEW MASTER(INPUT) // CREATE A NEW MASTER RECORD FOR THE INPUT
            //        FOR EACH MATCH IN MATCHES // FOR EACH OF THE DEFINITE MATCHES THAT WERE FOUND ADD THEM AS PROBABLE MATCHES TO THE INPUT
            //            INPUT.PROBABLE.ADD(MATCH);
            //        END FOR
            //END IF

            //// ANY PROBABLE MATCHES FROM THE MATCH SERVICE ARE JUST ADDED AS PROBABLES
            //FOR EACH PROB IN PROBABLES
            //        INPUT.PROBABLE.ADD(PROB);
            //END FOR
            List<IdentifiedData> insertData = new List<IdentifiedData>() { entity };
            var ignoreList = taggable.Tags.FirstOrDefault(o => o.TagKey == "mdm.ignore")?.Value.Split(';').AsEnumerable() ?? new string[0];

            // Existing probable links
            var existingProbableQuery = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={entity.Key}&relationshipType={MdmConstants.CandidateLocalRelationship}") }) as Expression;
            int tr = 0;
            var existingProbableLinks = relationshipService.Query(existingProbableQuery, 0, 100, out tr);

            // We want to obsolete any existing links that are no longer valid
            foreach (var er in existingProbableLinks.OfType<EntityRelationship>().Where(er => matchGroups[RecordMatchClassification.Match]?.Any(m => m == er.TargetEntityKey) == false && matchGroups[RecordMatchClassification.Probable]?.Any(m => m == er.TargetEntityKey) == false))
            {
                er.ObsoleteVersionSequenceId = Int32.MaxValue;
                insertData.Add(er);
            }
            foreach (var ar in existingProbableLinks.OfType<ActRelationship>().Where(ar => matchGroups[RecordMatchClassification.Match]?.Any(m => m == ar.TargetActKey) == false && matchGroups[RecordMatchClassification.Probable]?.Any(m => m == ar.TargetActKey) == false))
            {
                ar.ObsoleteVersionSequenceId = Int32.MaxValue;
                insertData.Add(ar);
            }

            // There is exactly one match and it is set to automerge
            if (matchGroups.ContainsKey(RecordMatchClassification.Match) && matchGroups[RecordMatchClassification.Match]?.Count() == 1
                && this.m_resourceConfiguration.AutoMerge)
            {
                // Next, ensure that the new master is set
                var master = matchGroups[RecordMatchClassification.Match].Single();
                
                // We want to remove all previous master matches
                var query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                    .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={entity.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                var rels = relationshipService.Query(query, 0, 100, out tr);
                // Are there any matches, then obsolete them and redirect to this master
                bool needsRelation = true;
                foreach (var r in rels)
                {
                    // Assign source entity
                    if (r is EntityRelationship)
                        (r as EntityRelationship).SourceEntity = entity as Entity;
                    else
                        (r as ActRelationship).SourceEntity = entity as Act;

                    // No changes needed to relationship just re-save
                    if ((r as EntityRelationship)?.TargetEntityKey == master || (r as ActRelationship)?.TargetActKey == master)
                    {
                        needsRelation = false;
                        insertData.Add(r as IdentifiedData);
                    }
                    else
                    {
                        if (r is EntityRelationship)
                            (r as EntityRelationship).ObsoleteVersionSequenceId = Int32.MaxValue;
                        else
                            (r as ActRelationship).ObsoleteVersionSequenceId = Int32.MaxValue;
                        // Cleanup old master
                        var oldMasterId = (Guid)r.GetType().GetQueryProperty("target").GetValue(r);
                        query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                            .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"target={oldMasterId}&source=!{entity.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                        relationshipService.Query(query, 0, 0, out tr);
                        if (tr == 0) // no other records point at the old master, obsolete it
                        {
                            var idt = typeof(IDataPersistenceService<>).MakeGenericType(typeof(Entity).IsAssignableFrom(typeof(T)) ? typeof(Entity) : typeof(Act));
                            var ids = ApplicationServiceContext.Current.GetService(idt) as IDataPersistenceService;
                            var oldMaster = ids.Get(oldMasterId) as IdentifiedData;
                            (oldMaster as IHasState).StatusConceptKey = StatusKeys.Obsolete;
                            insertData.Add(oldMaster);
                        }
                        insertData.Add(r as IdentifiedData);
                    }

                }

                if (needsRelation)
                    insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, entity, master));
                // dataService.Update(master);
                // No change in master
            }
            else
            {
                // We want to create a new master for this record?
                var query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                    .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={entity.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                var rels = relationshipService.Query(query, 0, 100, out tr);
                if (!rels.OfType<Object>().Any()) // There is no master
                {
                    var master = this.CreateMasterRecord();
                    insertData.Add(master as IdentifiedData);
                    insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, entity, master.Key));
                }
                else
                {
                    // Is this the only record in the current master relationship?
                    var oldMasterRel = rels.OfType<IdentifiedData>().SingleOrDefault().Clone();
                    var oldMasterId = (Guid)oldMasterRel.GetType().GetQueryProperty("target").GetValue(oldMasterRel);
                    ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove(oldMasterRel.Key.Value);

                    // Query for other masters
                    query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                    .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"target={oldMasterId}&source=!{entity.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                    relationshipService.Query(query, 0, 0, out tr);
                    if(tr > 0) // Old master has other records, we want to detach
                    {
                        var master = this.CreateMasterRecord();
                        insertData.Add(master as IdentifiedData);
                        insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, entity, master.Key));
                        if (oldMasterRel is EntityRelationship)
                            (oldMasterRel as EntityRelationship).ObsoleteVersionSequenceId = Int32.MaxValue;
                        else
                            (oldMasterRel as ActRelationship).ObsoleteVersionSequenceId = Int32.MaxValue;
                        insertData.Add(oldMasterRel);
                    } // otherwise we just leave the master 
                }
                // Now we persist
                if (matchGroups[RecordMatchClassification.Match] != null)
                    insertData.AddRange(matchGroups[RecordMatchClassification.Match]
                        .Where(m => !ignoreList.Contains(m.ToString())) // ignore list
                        .Where(m => !rels.OfType<EntityRelationship>().Any(er => er.TargetEntityKey == m)) // existing relationships
                        .Where(m => !rels.OfType<ActRelationship>().Any(er => er.TargetActKey == m)) // existing relationships
                        .Select(m => this.CreateRelationship(relationshipType, MdmConstants.CandidateLocalRelationship, entity, m)));
            }

            // Add probable records
            if (matchGroups[RecordMatchClassification.Probable] != null)
                insertData.AddRange(matchGroups[RecordMatchClassification.Probable]
                    .Where(m => !ignoreList.Contains(m.ToString())) // ignore list
                    .Select(m => this.CreateRelationship(relationshipType, MdmConstants.CandidateLocalRelationship, entity, m)));

            
            return new Bundle() { Item = insertData };
        }

        /// <summary>
        /// Get the master for the specified record
        /// </summary>
        private Guid? GetMaster(IdentifiedData match)
        {
            // Is the object already a master?
            var mdmType = match.LoadCollection<ITag>(nameof(ITaggable.Tags)).FirstOrDefault(o=>o.TagKey == "mdm.type")?.Value;
            Guid? retVal = null;
            switch(mdmType)
            {
                case "M": // master
                    retVal = match.Key;
                    break;
                case "T": // Record of truth , reverse find
                    if (match is Entity)
                        retVal = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Query(o => o.TargetEntityKey == match.Key && o.RelationshipTypeKey == MdmConstants.MasterRecordOfTruthRelationship, 0, 1, out int t, AuthenticationContext.SystemPrincipal).SingleOrDefault().SourceEntityKey;
                    else
                        retVal = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActRelationship>>().Query(o => o.TargetActKey == match.Key && o.RelationshipTypeKey == MdmConstants.MasterRecordOfTruthRelationship, 0, 1, out int t, AuthenticationContext.SystemPrincipal).SingleOrDefault().SourceEntityKey;
                    break;
                case "L": // local
                    if (match is Entity)
                        retVal = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Query(o => o.SourceEntityKey == match.Key && o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship, 0, 1, out int t, AuthenticationContext.SystemPrincipal).SingleOrDefault().TargetEntityKey;
                    else
                        retVal = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActRelationship>>().Query(o => o.SourceEntityKey == match.Key && o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship, 0, 1, out int t, AuthenticationContext.SystemPrincipal).SingleOrDefault().TargetActKey;
                    break;
                default:
                    this.m_traceSource.TraceWarning("Record {0} is an orphan and has not master, will create one");
                    if (match is Entity) {
                        var master = this.CreateMasterRecord() as IdentifiedData;
                        var bundle = new Bundle()
                        {
                            Item = new List<IdentifiedData>()
                            {
                                master,
                                this.CreateRelationship(typeof(EntityRelationship), MdmConstants.MasterRecordRelationship, (T)match, master.Key)
                            }
                        };
                        this.m_persistence.Insert(bundle, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                        retVal = master.Key;
                    }
                    else
                    {
                        var master = this.CreateMasterRecord() as IdentifiedData;
                        var bundle = new Bundle()
                        {
                            Item = new List<IdentifiedData>()
                            {
                                master,
                                this.CreateRelationship(typeof(ActRelationship), MdmConstants.MasterRecordRelationship, (T)match, master.Key)
                            }
                        };
                        this.m_persistence.Insert(bundle, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                        retVal = master.Key;
                    }
                    
                    break;
            }
            return retVal;

        }

        /// <summary>
        /// Create a relationship of the specied type
        /// </summary>
        /// <param name="relationshipType"></param>
        /// <param name="relationshipClassification"></param>
        /// <param name="sourceEntity"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        private IdentifiedData CreateRelationship(Type relationshipType, Guid relationshipClassification, T sourceEntity, Guid? targetEntity)
        {
            var relationship = Activator.CreateInstance(relationshipType, relationshipClassification, targetEntity) as IdentifiedData;
            relationship.Key = Guid.NewGuid();
            (relationship as ISimpleAssociation).SourceEntityKey = sourceEntity.Key;
            (relationship as ISimpleAssociation).SourceEntity = sourceEntity;
            return relationship;
        }

        /// <summary>
        /// Create a master record from the specified local records
        /// </summary>
        /// <param name="localRecords">The local records that are to be used to generate a master record</param>
        /// <returns>The created master record</returns>
        private IMdmMaster<T> CreateMasterRecord()
        {
            var mtype = typeof(Entity).IsAssignableFrom(typeof(T)) ? typeof(EntityMaster<>) : typeof(ActMaster<>);
            var retVal = Activator.CreateInstance(mtype.MakeGenericType(typeof(T))) as IMdmMaster<T>;
            retVal.Key = Guid.NewGuid();
            retVal.VersionKey = null;
            (retVal as BaseEntityData).CreatedByKey = Guid.Parse(AuthenticationContext.SystemApplicationSid);
            return retVal;
        }

        /// <summary>
        /// Dispose this object (unsubscribe)
        /// </summary>
        public override void Dispose()
        {
            if (this.m_repository != null)
            {
                this.m_repository.Inserting -= this.OnPrePersistenceValidate;
                this.m_repository.Saving -= this.OnPrePersistenceValidate;
                this.m_repository.Inserting -= this.OnInserting;
                this.m_repository.Saving -= this.OnSaving;
                this.m_repository.Retrieved -= this.OnRetrieved;
                this.m_repository.Retrieving -= this.OnRetrieving;
                this.m_repository.Obsoleting -= this.OnObsoleting;
                this.m_repository.Querying -= this.OnQuerying;
                this.m_repository.Queried -= this.OnQueried;
            }
        }

        /// <summary>
        /// Instructs the MDM service to merge the specified master with the linked duplicates
        /// </summary>
        /// <param name="master"></param>
        /// <param name="linkedDuplicates"></param>
        /// <returns></returns>
        public virtual T Merge(T master, IEnumerable<T> linkedDuplicates)
        {

            DataMergingEventArgs<T> preEventArgs = new DataMergingEventArgs<T>(master, linkedDuplicates);
            this.Merging?.Invoke(this, preEventArgs);
            if(preEventArgs.Cancel)
            {
                this.m_traceSource.TraceInfo("Pre-event handler has indicated a cancel of merge on {0}", master);
                return null;
            }
            master = preEventArgs.Master; // Allow resource to update these fields
            linkedDuplicates = preEventArgs.Linked;

            // Relationship type
            var relationshipType = master is Entity ? typeof(EntityRelationship) : typeof(ActRelationship);
            var relationshipService = ApplicationServiceContext.Current.GetService(typeof(IDataPersistenceService<>).MakeGenericType(relationshipType)) as IDataPersistenceService;

            // Ensure that MASTER is in fact a master
            IDataPersistenceService masterService = master is Entity ? ApplicationServiceContext.Current.GetService<IDataPersistenceService<Entity>>() as IDataPersistenceService : ApplicationServiceContext.Current.GetService<IDataPersistenceService<Act>>() as IDataPersistenceService;
            var masterData = masterService.Get(master.Key.Value) as IClassifiable;
            if (masterData.ClassConceptKey == MdmConstants.MasterRecordClassification && !AuthenticationContext.Current.Principal.IsInRole("SYSTEM"))
                new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.WriteMdmMaster).Demand();
            else
            {
                this.EnsureProvenance(master, AuthenticationContext.Current.Principal);
                var existingMasterQry = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                           .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={master.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                int tr = 0;
                var masterRel = relationshipService.Query(existingMasterQry, 0, 2, out tr).OfType<ISimpleAssociation>().SingleOrDefault();
                masterData = masterService.Get((Guid)masterRel.GetType().GetQueryProperty("target").GetValue(masterRel)) as IClassifiable;
            }

            // For each of the linked duplicates we want to get the master relationships 
            foreach (var ldpl in linkedDuplicates)
            {
                // Is the linked duplicate a master record?
                var linkedClass = masterService.Get(ldpl.Key.Value) as IClassifiable;
                if (linkedClass.ClassConceptKey == MdmConstants.MasterRecordClassification && !AuthenticationContext.Current.Principal.IsInRole("SYSTEM"))
                    new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.MergeMdmMaster).Demand();
                else
                    this.EnsureProvenance(ldpl, AuthenticationContext.Current.Principal);

                // Allowed merges
                // LOCAL > MASTER - A local record is being merged into a MASTER
                // MASTER > MASTER - Two MASTER records are being merged (administrative merge)
                // LOCAL > LOCAL - Two LOCAL records are being merged
                if (linkedClass.ClassConceptKey == masterData.ClassConceptKey)
                {
                    if (linkedClass.ClassConceptKey == MdmConstants.MasterRecordClassification) // MASTER <> MASTER
                    {
                        // First, we move all references from the subsumed MASTER to the new MASTER
                        var existingMasterQry = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                            .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"target={ldpl.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                        int tr = 0;
                        var existingMasters = relationshipService.Query(existingMasterQry, 0, 100, out tr);
                        foreach (var erel in existingMasters)
                        {
                            erel.GetType().GetQueryProperty("target").SetValue(erel, master.Key);
                            relationshipService.Update(erel);
                        }

                        // Now we want to mark LMASTER replaced by MASTER
                        var mrel = this.CreateRelationship(relationshipType, EntityRelationshipTypeKeys.Replaces, master, ldpl.Key);
                        relationshipService.Insert(mrel);
                        ApplicationServiceContext.Current.GetService<IDataPersistenceService<T>>().Obsolete(ldpl, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                    }
                    else // LOCAL <> LOCAL
                    {
                        // With local to local we want to remove the existing MASTER from the replaced local and redirect it to the MASTER of the new local
                        var existingMasterQry = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                            .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={ldpl.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                        int tr = 0;
                        var localMaster = relationshipService.Query(existingMasterQry, 0, 2, out tr).OfType<ISimpleAssociation>().SingleOrDefault();

                        Guid oldMaster = (Guid)localMaster.GetType().GetQueryProperty("target").GetValue(localMaster);
                        // Now we want to move the local master to the master of the MASTER LOCAL
                        localMaster.GetType().GetQueryProperty("target").SetValue(localMaster, (masterData as IdentifiedData).Key);
                        relationshipService.Update(localMaster);

                        // Now we want to set replaces relationship
                        var mrel = this.CreateRelationship(relationshipType, EntityRelationshipTypeKeys.Replaces, master, ldpl.Key);
                        relationshipService.Insert(mrel);
                        ApplicationServiceContext.Current.GetService<IDataPersistenceService<T>>().Obsolete(ldpl, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

                        // Check if the master is orphaned, if so obsolete it
                        existingMasterQry = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                            .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"target={oldMaster}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                        var otherMaster = relationshipService.Query(existingMasterQry, 0, 0, out tr);
                        if (tr == 0)
                            masterService.Obsolete(masterService.Get(oldMaster));
                    }
                }
                // LOCAL > MASTER
                else if (masterData.ClassConceptKey == MdmConstants.MasterRecordClassification &&
                    linkedClass.ClassConceptKey != MdmConstants.MasterRecordClassification)
                {
                    // LOCAL to MASTER is merged as removing all probables and assigning the MASTER relationship from the 
                    // existing master to the identified master
                    var existingQuery = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                           .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={ldpl.Key}&relationshipType={MdmConstants.MasterRecordRelationship}&relationshipType={MdmConstants.CandidateLocalRelationship}") }) as Expression;
                    int tr = 0;
                    var existingRelationships = relationshipService.Query(existingQuery, 0, null, out tr);
                    var oldMaster = Guid.Empty;
                    // Remove existing relationships
                    foreach (var rel in existingRelationships)
                    {
                        if (rel.GetType().GetQueryProperty("relationshipType").GetValue(rel).Equals(MdmConstants.MasterRecordClassification))
                            oldMaster = (Guid)rel.GetType().GetQueryProperty("target").GetValue(rel);
                        relationshipService.Obsolete(rel);
                    }

                    // Add relationship 
                    relationshipService.Insert(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, ldpl, master.Key));

                    // Obsolete the old master
                    existingQuery = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                            .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"target={oldMaster}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                    var otherMaster = relationshipService.Query(existingQuery, 0, 0, out tr);
                    if (tr == 0)
                        masterService.Obsolete(masterService.Get(oldMaster));
                }
                else
                    throw new InvalidOperationException("Invalid merge. Only LOCAL>MASTER, MASTER>MASTER or LOCAL>LOCAL are supported");
            }

            T retVal = default(T);
            if (masterData.ClassConceptKey == MdmConstants.MasterRecordClassification)
                retVal = masterData is Entity ? new EntityMaster<T>((Entity)masterService.Get(master.Key.Value)).GetMaster(AuthenticationContext.Current.Principal) :
                    new ActMaster<T>((Act)masterService.Get(master.Key.Value)).GetMaster(AuthenticationContext.Current.Principal);
            else 
                retVal = (T)masterService.Get(master.Key.Value);

            this.Merged?.Invoke(this, new DataMergeEventArgs<T>(retVal, linkedDuplicates));
            return retVal;
        }

        /// <summary>
        /// Ensures that <paramref name="master"/> is owned by application granted by <paramref name="principal"/>
        /// </summary>
        /// <param name="master"></param>
        /// <param name="principal"></param>
        private void EnsureProvenance(T master, IPrincipal principal)
        {
            var provenance = (master as BaseEntityData)?.LoadProperty<SecurityProvenance>("CreatedBy");
            var claimsPrincipal = principal as IClaimsPrincipal;
            var applicationPrincipal = claimsPrincipal.Identities.OfType<Core.Security.ApplicationIdentity>().SingleOrDefault();
            if (applicationPrincipal != null &&
                applicationPrincipal.Name != provenance?.LoadProperty<SecurityApplication>("Application")?.Name // was not the original author
                || !AuthenticationContext.Current.Principal.IsInRole("SYSTEM"))
                new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.ReadMdmLocals).Demand();
        }

        /// <summary>
        /// Instructs the MDM service to unmerge the specified master from unmerge duplicate
        /// </summary>
        /// <param name="master"></param>
        /// <param name="unmergeDuplicate"></param>
        /// <returns></returns>
        public virtual T Unmerge(T master, T unmergeDuplicate)
        {
            throw new NotImplementedException();
        }
    }
}