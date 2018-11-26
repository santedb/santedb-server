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
 * Date: 2018-9-25
 */
using SanteDB.Core;
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
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Services;
using SanteDB.Persistence.MDM.Configuration;
using SanteDB.Persistence.MDM.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
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

        // Configuration
        private MdmResourceConfiguration m_resourceConfiguration;

        // Tracer
        private TraceSource m_traceSource = new TraceSource(MdmConstants.TraceSourceName);

        // The repository that this listener is attached to
        private IDataPersistenceService<T> m_repository;

        /// <summary>
        /// Resource listener
        /// </summary>
        public MdmResourceListener(MdmResourceConfiguration configuration)
        {
            this.m_resourceConfiguration = configuration;
            this.m_repository = ApplicationServiceContext.Current.GetService<IDataPersistenceService<T>>();
            if (this.m_repository == null)
                throw new InvalidOperationException($"Could not find persistence service for {typeof(T)}");

            // Subscribe
            this.m_repository.Inserting += this.OnPrePersistenceValidate;
            this.m_repository.Updating += this.OnPrePersistenceValidate;
            this.m_repository.Inserted += this.OnInserted;
            this.m_repository.Updated += this.OnUpdated;
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

            // The query is already querying for master records
            if (query.ContainsKey("classConcept") && query["classConcept"].Contains(MdmConstants.MasterRecordClassification.ToString()))
                return;
            // The query doesn't contain a query for master records, so...
            // If the user is not in the role "SYSTEM" OR they didn't ask specifically for LOCAL records we have to rewrite the query to use MASTER
            if (!e.Principal.IsInRole("SYSTEM") || !query.ContainsKey("tag[mdm.type].value"))
            {
                // Did the person ask specifically for a local record? if so we need to demand permission
                if (query.ContainsKey("tag[mdm.type].value") && query["tag[mdm.type].value"].Contains("L"))
                    new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.ReadMdmLocals).Demand();
                else // We want to modify the query to only include masters and rewrite the query
                {
                    query = new NameValueCollection(query.ToDictionary(o => $"relationship[MDM-Master].source@{typeof(T).Name}.{o.Key}", o => o.Value));
                    query.Add("classConcept", MdmConstants.MasterRecordClassification.ToString());
                    e.Cancel = true; // We want to cancel the other's query

                    // We are wrapping an entity, so we query entity masters
                    int tr = 0;
                    if (typeof(Entity).IsAssignableFrom(typeof(T)))
                        e.Results = this.MasterQuery<Entity>(query, e.QueryId.GetValueOrDefault(), e.Offset, e.Count, e.Principal, out tr);
                    else
                        e.Results = this.MasterQuery<Act>(query, e.QueryId.GetValueOrDefault(), e.Offset, e.Count, e.Principal, out tr);
                    e.TotalResults = tr;
                }
            }
        }

        /// <summary>
        /// Perform a master query 
        /// </summary>
        private IEnumerable<T> MasterQuery<TMasterType>(NameValueCollection query, Guid queryId, int offset, int? count, IPrincipal principal, out int totalResults)
            where TMasterType : IdentifiedData
        {
            var masterQuery = QueryExpressionParser.BuildLinqExpression<TMasterType>(query);
            int tr = 0;
            return ApplicationServiceContext.Current.GetService<IStoredQueryDataPersistenceService<TMasterType>>().Query(masterQuery, queryId, offset, count, out totalResults, principal)
                .Select(o => o is Entity ? new EntityMaster<T>((Entity)(object)o).GetMaster(principal) : new ActMaster<T>((Act)(Object)o).GetMaster(principal)).AsParallel().ToList();
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
            if ((sender as IDataPersistenceService<T>).Count(o => o.Key == e.Id, AuthenticationContext.Current.Principal) == 0) //
            {
                e.Cancel = true;
                if (typeof(Entity).IsAssignableFrom(typeof(T)))
                {
                    var master = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Entity>>().Get(e.Id.Value, null, false, AuthenticationContext.Current.Principal);
                    e.OverrideResult = new EntityMaster<T>(master).GetMaster(AuthenticationContext.Current.Principal);
                }
                else if (typeof(Act).IsAssignableFrom(typeof(T)))
                {
                    var master = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Act>>().Get(e.Id.Value, null, false, AuthenticationContext.Current.Principal);
                    e.OverrideResult = new ActMaster<T>(master).GetMaster(AuthenticationContext.Current.Principal);
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
                var eRelationship = (e.Data as Entity).LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship);
                if (eRelationship != null)
                {
                    // Get existing er if available
                    var dbRelationship = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Query(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship && o.SourceEntityKey == identified.Key, 0, 1, out tr, e.Principal);
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
                var eRelationship = (e.Data as Act).LoadCollection<ActRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship);
                if (eRelationship != null)
                {
                    // Get existing er if available
                    var dbRelationship = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActRelationship>>().Query(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship && o.SourceEntityKey == identified.Key, 0, 1, out tr, e.Principal);
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
        /// Fired after record is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>This method will ensure that the record matching service is re-run after an update, and 
        /// that any new links be created/updated.</remarks>
        protected virtual void OnUpdated(object sender, DataPersistedEventArgs<T> e)
        {

            // Is this object a ROT, if it is then we do not perform any changes to re-binding
            var taggable = e.Data as ITaggable;
            var identified = e.Data as IIdentifiedEntity;
            var mdmTag = taggable?.Tags.FirstOrDefault(o => o.TagKey == "mdm.type");
            if (mdmTag?.Value == "T")
                return; // Record of truth is never re-matched and remains bound to the original object
            else if (mdmTag?.Value != "M") // record is a local and may need to be re-matched
            {
                if (identified is Entity && mdmTag == null)
                {
                    var et = new EntityTag("mdm.type", "L");
                    ApplicationServiceContext.Current.GetService<ITagPersistenceService>().Save(identified.Key.Value, et);
                    (e.Data as Entity).Tags.Add(et);
                    mdmTag = et;
                }
                else if (identified is Act && mdmTag == null)
                {
                    var at = new ActTag("mdm.type", "L");
                    ApplicationServiceContext.Current.GetService<ITagPersistenceService>().Save(identified.Key.Value, at);
                    (e.Data as Act).Tags.Add(at);
                    mdmTag = at;
                }

                // Perform matching
                this.PerformMdmMatch(identified);
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
        protected virtual void OnInserted(object sender, DataPersistedEventArgs<T> e)
        {

            // Gather tags to determine whether the object has been linked to a master
            var taggable = e.Data as ITaggable;
            var identified = e.Data as IIdentifiedEntity;
            var mdmTag = taggable?.Tags.FirstOrDefault(o => o.TagKey == "mdm.type");
            if (mdmTag?.Value != "M" && mdmTag?.Value != "T") // Record is a master - MASTER duplication on input is rare
            {
                if (identified is Entity)
                {
                    var et = new EntityTag("mdm.type", "L");
                    ApplicationServiceContext.Current.GetService<ITagPersistenceService>().Save(identified.Key.Value, et);
                    (e.Data as Entity).Tags.Add(et);
                    mdmTag = et;
                }
                else if (identified is Act)
                {
                    var at = new ActTag("mdm.type", "L");
                    ApplicationServiceContext.Current.GetService<ITagPersistenceService>().Save(identified.Key.Value, at);
                    (e.Data as Act).Tags.Add(at);
                    mdmTag = at;
                }
            }

            // Find records for which this record could be match // async
            if (mdmTag?.Value == "L")
                this.PerformMdmMatch(identified);
                //ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem(this.PerformMdmMatch, identified);

        }

        /// <summary>
        /// Perform an MDM match process to link the probable and definitive match
        /// </summary>
        private void PerformMdmMatch(object state)
        {
            var matchService = ApplicationServiceContext.Current.GetService<IRecordMatchingService>();
            if (matchService == null)
                return; // Cannot make determination of matching

            var identified = (IdentifiedData)state;
            var taggable = (ITaggable)state;
            var relationshipType = identified is Entity ? typeof(EntityRelationship) : typeof(ActRelationship);
            var relationshipService = ApplicationServiceContext.Current.GetService(typeof(IDataPersistenceService<>).MakeGenericType(relationshipType)) as IDataPersistenceService;
            var matchingRecords = matchService.Match(identified, this.m_resourceConfiguration.MatchConfiguration);

            // Matching records can only match with MASTER records
            matchingRecords = matchingRecords.Where(o => (o.Record as ITaggable)?.Tags.Any(t => t.TagKey == "mdm.type" && t.Value == "M") == true);
            var matchGroups = matchingRecords
                .GroupBy(o => o.Classification)
                .ToDictionary(o => o.Key, o => o);
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
            List<IdentifiedData> insertData = new List<IdentifiedData>();
            var ignoreList = taggable.Tags.FirstOrDefault(o => o.TagKey == "mdm.ignore")?.Value.Split(';').AsEnumerable() ?? new string[0];

            // Existing probable links
            var existingProbableQuery = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={identified.Key}&relationshipType={MdmConstants.DuplicateRecordRelationship}") }) as Expression;
            int tr = 0;
            var existingProbableLinks = relationshipService.Query(existingProbableQuery, 0, 100, out tr);

            // We want to obsolete any existing links that are no longer valid
            foreach (var er in existingProbableLinks.OfType<EntityRelationship>().Where(er => matchGroups[RecordMatchClassification.Match]?.Any(m => m.Record.Key == er.TargetEntityKey) == false && matchGroups[RecordMatchClassification.Probable]?.Any(m => m.Record.Key == er.TargetEntityKey) == false))
                relationshipService.Obsolete(er);
            foreach (var ar in existingProbableLinks.OfType<ActRelationship>().Where(ar => matchGroups[RecordMatchClassification.Match]?.Any(m => m.Record.Key == ar.TargetActKey) == false && matchGroups[RecordMatchClassification.Probable]?.Any(m => m.Record.Key == ar.TargetActKey) == false))
                relationshipService.Obsolete(ar);

            // There is exactly one match and it is set to automerge
            if (matchGroups.ContainsKey(RecordMatchClassification.Match) && matchGroups[RecordMatchClassification.Match]?.Count() == 1
                && this.m_resourceConfiguration.AutoMerge)
            {
                // Next, ensure that the new master is set
                var master = matchGroups[RecordMatchClassification.Match].Single().Record;

                // We want to remove all previous master matches
                var query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                    .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={identified.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                var rels = relationshipService.Query(query, 0, 100, out tr);
                // Are there any matches, then obsolete them and redirect to this master
                bool needsRelation = true;
                foreach (var r in rels)
                {
                    if ((r as EntityRelationship)?.TargetEntityKey == master.Key || (r as ActRelationship)?.TargetActKey == master.Key)
                    {
                        needsRelation = false;
                        if (r is EntityRelationship)
                            (r as EntityRelationship).SourceEntity = state as Entity;
                        else
                            (r as ActRelationship).SourceEntity = state as Act;

                        relationshipService.Update(r);
                    }
                    else
                    {
                        relationshipService.Obsolete(r);
                        // Cleanup old master
                        var oldMasterId = (Guid)r.GetType().GetQueryProperty("target").GetValue(r);
                        query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                            .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"target={oldMasterId}&source=!{identified.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                        relationshipService.Query(query, 0, 0, out tr);
                        if (tr == 0) // no other records point at the old master, obsolete it
                        {
                            var idt = typeof(IDataPersistenceService<>).MakeGenericType(typeof(Entity).IsAssignableFrom(typeof(T)) ? typeof(Entity) : typeof(Act));
                            var ids = ApplicationServiceContext.Current.GetService(idt) as IDataPersistenceService;
                            ids.Obsolete(ids.Get(oldMasterId));
                        }
                    }
                }

                if (needsRelation)
                    insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, identified.Key, master.Key, (identified as IVersionedEntity).VersionSequence));
                // dataService.Update(master);
                // No change in master
            }
            else
            {
                // We want to create a new master for this record?
                var query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                    .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={identified.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                var rels = relationshipService.Query(query, 0, 100, out tr);
                if (!rels.OfType<Object>().Any()) // There is no master
                {
                    var master = this.CreateMasterRecord();
                    insertData.Add(master as IdentifiedData);
                    insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, identified.Key, master.Key, (identified as IVersionedEntity).VersionSequence));
                }
                else
                {
                    // Is this the only record in the current master relationship?
                    var oldMasterRel = rels.OfType<IdentifiedData>().SingleOrDefault().Clone();
                    var oldMasterId = (Guid)oldMasterRel.GetType().GetQueryProperty("target").GetValue(oldMasterRel);
                    ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove(oldMasterRel.Key.Value);

                    // Query for other masters
                    query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                    .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"target={oldMasterId}&source=!{identified.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                    relationshipService.Query(query, 0, 0, out tr);
                    if(tr > 0) // Old master has other records, we want to detach
                    {
                        var master = this.CreateMasterRecord();
                        insertData.Add(master as IdentifiedData);
                        insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, identified.Key, master.Key, (identified as IVersionedEntity).VersionSequence));
                        relationshipService.Obsolete(oldMasterRel);
                    } // otherwise we just leave the master 
                }
                // Now we persist
                if (matchGroups[RecordMatchClassification.Match] != null)
                    insertData.AddRange(matchGroups[RecordMatchClassification.Match]
                        .Where(m => !ignoreList.Contains(m.Record.Key.Value.ToString())) // ignore list
                        .Where(m => !rels.OfType<EntityRelationship>().Any(er => er.TargetEntityKey == m.Record.Key)) // existing relationships
                        .Where(m => !rels.OfType<ActRelationship>().Any(er => er.TargetActKey == m.Record.Key)) // existing relationships
                        .Select(m => this.CreateRelationship(relationshipType, MdmConstants.DuplicateRecordRelationship, identified.Key, m.Record.Key, (identified as IVersionedEntity).VersionSequence)));
            }

            // Add probable records
            if (matchGroups[RecordMatchClassification.Probable] != null)
                insertData.AddRange(matchGroups[RecordMatchClassification.Probable]
                    .Where(m => !ignoreList.Contains(m.Record.Key.Value.ToString())) // ignore list
                    .Select(m => this.CreateRelationship(relationshipType, MdmConstants.DuplicateRecordRelationship, identified.Key, m.Record.Key, (identified as IVersionedEntity).VersionSequence)));

            // Insert relationships
            ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Insert(new Bundle()
            {
                Item = insertData
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
        }

        /// <summary>
        /// Create a relationship of the specied type
        /// </summary>
        /// <param name="relationshipType"></param>
        /// <param name="relationshipClassification"></param>
        /// <param name="sourceEntity"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        private IdentifiedData CreateRelationship(Type relationshipType, Guid relationshipClassification, Guid? sourceEntity, Guid? targetEntity, Int32? versionSequence)
        {
            var relationship = Activator.CreateInstance(relationshipType, relationshipClassification, targetEntity) as IdentifiedData;
            relationship.Key = Guid.NewGuid();
            (relationship as ISimpleAssociation).SourceEntityKey = sourceEntity;
            (relationship as IVersionedAssociation).EffectiveVersionSequenceId = versionSequence;
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
                this.m_repository.Updating -= this.OnPrePersistenceValidate;
                this.m_repository.Inserted -= this.OnInserted;
                this.m_repository.Updated -= this.OnUpdated;
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
                        var mrel = this.CreateRelationship(relationshipType, EntityRelationshipTypeKeys.Replaces, master.Key, ldpl.Key, (master as IVersionedEntity).VersionSequence);
                        relationshipService.Insert(mrel);
                        ApplicationServiceContext.Current.GetService<IDataPersistenceService<T>>().Obsolete(ldpl, TransactionMode.Commit);
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
                        var mrel = this.CreateRelationship(relationshipType, EntityRelationshipTypeKeys.Replaces, master.Key, ldpl.Key, (master as IVersionedEntity).VersionSequence);
                        relationshipService.Insert(mrel);
                        ApplicationServiceContext.Current.GetService<IDataPersistenceService<T>>().Obsolete(ldpl, TransactionMode.Commit);

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
                           .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={ldpl.Key}&relationshipType={MdmConstants.MasterRecordRelationship}&relationshipType={MdmConstants.DuplicateRecordRelationship}") }) as Expression;
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
                    relationshipService.Insert(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, ldpl.Key, master.Key, (master as IVersionedEntity).VersionSequence));

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

            if (masterData.ClassConceptKey == MdmConstants.MasterRecordClassification)
                return masterData is Entity ? new EntityMaster<T>((Entity)masterService.Get(master.Key.Value)).GetMaster(AuthenticationContext.Current.Principal) :
                    new ActMaster<T>((Act)masterService.Get(master.Key.Value)).GetMaster(AuthenticationContext.Current.Principal);
            else
                return (T)masterService.Get(master.Key.Value);
        }

        /// <summary>
        /// Ensures that <paramref name="master"/> is owned by application granted by <paramref name="principal"/>
        /// </summary>
        /// <param name="master"></param>
        /// <param name="principal"></param>
        private void EnsureProvenance(T master, IPrincipal principal)
        {
            var provenance = (master as BaseEntityData)?.LoadProperty<SecurityProvenance>("CreatedBy");
            var claimsPrincipal = principal as ClaimsPrincipal;
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