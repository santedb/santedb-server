using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Data;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Services;
using SanteDB.Persistence.MDM.Configuration;
using SanteDB.Persistence.MDM.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
            this.m_repository = ApplicationContext.Current.GetService<IDataPersistenceService<T>>();
            if (this.m_repository == null)
                throw new InvalidOperationException($"Could not find persistence service for {typeof(T)}");

            // Subscribe
            this.m_repository.Inserting += this.PrePersistenceValidate;
            this.m_repository.Updating += this.PrePersistenceValidate;
            this.m_repository.Inserted += this.Inserted;
            this.m_repository.Updated += this.Updated;
            this.m_repository.Obsoleting += this.Obsoleting;
            this.m_repository.Retrieved += this.Retrieved;
            this.m_repository.Queried += this.Queried;
            this.m_repository.Querying += this.Querying;
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
        private void Querying(object sender, MARC.HI.EHRS.SVC.Core.Event.PreQueryEventArgs<T> e)
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
                        e.OverrideResults = this.MasterQuery<Entity>(query, e.QueryId.GetValueOrDefault(), e.Offset, e.Count, e.Principal, out tr);
                    else
                        e.OverrideResults = this.MasterQuery<Act>(query, e.QueryId.GetValueOrDefault(), e.Offset, e.Count, e.Principal, out tr);
                    e.OverrideTotalResults = tr;
                }
            }
        }

        /// <summary>
        /// Perform a master query 
        /// </summary>
        private IEnumerable<T> MasterQuery<TMasterType>(NameValueCollection query, Guid queryId, int offset, int? count, IPrincipal principal, out int totalResults)
        {
            var masterQuery = QueryExpressionParser.BuildLinqExpression<TMasterType>(query);
            int tr = 0;
            return ApplicationContext.Current.GetService<IStoredQueryDataPersistenceService<TMasterType>>().Query(masterQuery, queryId, offset, count, principal, out totalResults)
                .Select(o => o is Entity ? new EntityMaster<T>((Entity)(object)o).GetMaster(principal) : new ActMaster<T>((Act)(Object)o).GetMaster(principal)).AsParallel().ToList();
        }

        /// <summary>
        /// Handles when a subscribed object is queried
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>The MDM provider will ensure that no data from the LOCAL instance which is masked is returned
        /// in the MASTER record</remarks>
        private void Queried(object sender, MARC.HI.EHRS.SVC.Core.Event.PostQueryEventArgs<T> e)
        {
            // TODO: Filter master record data based on taboo child records.
            
        }

        /// <summary>
        /// Handles when subscribed object is retrieved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>MDM provider will ensure that if the retrieved record is a MASTER record, that no 
        /// data from masked LOCAL records is included.</remarks>
        private void Retrieved(object sender, MARC.HI.EHRS.SVC.Core.Event.PostRetrievalEventArgs<T> e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validates that a MASTER record is only being inserted by this class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>We don't want clients submitting MASTER records, so this method will ensure that all records
        /// being sent with tag of MASTER are indeed sent by the MDM or SYSTEM user.</remarks>
        private void PrePersistenceValidate(object sender, MARC.HI.EHRS.SVC.Core.Event.PrePersistenceEventArgs<T> e)
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
                var eRelationship = (e.Data as Entity).Relationships.FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship);
                if (eRelationship != null)
                {
                    // Get existing er if available
                    var dbRelationship = ApplicationContext.Current.GetService<IDataPersistenceService<EntityRelationship>>().Query(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship && o.SourceEntityKey == identified.Key, 0, 1, e.Principal, out tr);
                    if (tr == 0 || dbRelationship.First().TargetEntityKey == eRelationship.TargetEntityKey)
                        return;
                    else if (!e.Principal.IsInRole("SYSTEM")) // The target entity is being re-associated make sure the principal is allowed to do this
                        new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.WriteMdmMaster).Demand();
                }
            }
            else if (e.Data is Act)
            {
                var eRelationship = (e.Data as Act).Relationships.FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship);
                if (eRelationship != null)
                {
                    // Get existing er if available
                    var dbRelationship = ApplicationContext.Current.GetService<IDataPersistenceService<ActRelationship>>().Query(o => o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship || o.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship && o.SourceEntityKey == identified.Key, 0, 1, e.Principal, out tr);
                    if (tr == 0 || dbRelationship.First().TargetActKey == eRelationship.TargetActKey)
                        return;
                    else if (!e.Principal.IsInRole("SYSTEM")) // The target entity is being re-associated make sure the principal is allowed to do this
                        new PolicyPermission(PermissionState.Unrestricted, MdmPermissionPolicyIdentifiers.WriteMdmMaster).Demand();
                }
            }
        }

        /// <summary>
        /// Fired before a record is obsoleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>We don't want a MASTER record to be obsoleted under any condition. MASTER records require special permission to 
        /// obsolete and also require that all LOCAL records be either re-assigned or obsoleted as well.</remarks>
        private void Obsoleting(object sender, MARC.HI.EHRS.SVC.Core.Event.PrePersistenceEventArgs<T> e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Fired after record is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>This method will ensure that the record matching service is re-run after an update, and 
        /// that any new links be created/updated.</remarks>
        private void Updated(object sender, MARC.HI.EHRS.SVC.Core.Event.PostPersistenceEventArgs<T> e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Fired after record is inserted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>This method will fire the record matching service and will ensure that duplicates are marked
        /// and merged into any existing MASTER record.</remarks>
        private void Inserted(object sender, MARC.HI.EHRS.SVC.Core.Event.PostPersistenceEventArgs<T> e)
        {

            var matchService = ApplicationContext.Current.GetService<IRecordMatchingService>();
            if (matchService == null)
                return; // Cannot make determination of matching

            // Is this not a master?
            var taggable = e.Data as ITaggable;
            var identified = e.Data as IIdentifiedEntity;
            var mdmTag = taggable?.Tags.FirstOrDefault(o => o.TagKey == "mdm.type");
            if (mdmTag?.Value != "M") // Record is a master - MASTER duplication on input is rare
            {
                if (identified is Entity)
                {
                    var et = new EntityTag("mdm.type", "L");
                    ApplicationContext.Current.GetService<ITagPersistenceService>().Save(identified.Key.Value, et);
                    (e.Data as Entity).Tags.Add(et);
                    mdmTag = et;
                }
                else if (identified is Act)
                {
                    var at = new ActTag("mdm.type", "L");
                    ApplicationContext.Current.GetService<ITagPersistenceService>().Save(identified.Key.Value, at);
                    (e.Data as Act).Tags.Add(at);
                    mdmTag = at;
                }
            }
            else
                throw new InvalidOperationException("Cannot insert synthetic master record");

            // Find records for which this record could be match // async
            var relationshipType = identified is Entity ? typeof(EntityRelationship) : typeof(ActRelationship);
            var relationshipService = ApplicationContext.Current.GetService(typeof(IDataPersistenceService<>).MakeGenericType(relationshipType)) as IDataPersistenceService;
            var matchingRecords = matchService.Match(e.Data, this.m_resourceConfiguration.MatchConfiguration);

            // Matching records can only match with MASTER records
            matchingRecords = matchingRecords.Where(o => (o.Record as ITaggable)?.Tags.Any(t => t.TagKey == "mdm.type" && t.Value == "M") == true);
            var matchGroups = matchingRecords
                .GroupBy(o => o.Classification)
                .ToDictionary(o => o.Key, o => o);
            
            
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
            if(mdmTag.Value == "L")
            {

                List<IdentifiedData> insertData = new List<IdentifiedData>();

                // There is exactly one match and it is set to automerge
                if(matchGroups.ContainsKey(RecordMatchClassification.Match) && matchGroups[RecordMatchClassification.Match].Count() == 1 
                    && this.m_resourceConfiguration.AutoMerge)
                {
                    var master = matchGroups[RecordMatchClassification.Match].First().Record;
                    var dataService = ApplicationContext.Current.GetService<IDataPersistenceService<T>>() as IDataPersistenceService;
                    insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, identified.Key, master.Key, (identified as IVersionedEntity).VersionSequence));
                    dataService.Update(master);
                }
                else
                {
                    // We want to create a new master for this record
                    var master = this.CreateMasterRecord();
                    insertData.Add(master as IdentifiedData);
                    insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, identified.Key, master.Key, (identified as IVersionedEntity).VersionSequence));

                    // Now add each of the found masters to the probable list
                    if(matchGroups.ContainsKey(RecordMatchClassification.Match)) insertData.AddRange(matchGroups[RecordMatchClassification.Match].Select(m => this.CreateRelationship(relationshipType, MdmConstants.DuplicateRecordRelationship, identified.Key, m.Record.Key, (identified as IVersionedEntity).VersionSequence)));
                }

                // Add probable records
                if (matchGroups.ContainsKey(RecordMatchClassification.Probable)) insertData.AddRange(matchGroups[RecordMatchClassification.Probable].Select(m => this.CreateRelationship(relationshipType, MdmConstants.DuplicateRecordRelationship, identified.Key, m.Record.Key, (identified as IVersionedEntity).VersionSequence)));

                // Insert relationships
                ApplicationContext.Current.GetService<IDataPersistenceService<Bundle>>().Insert(new Bundle()
                {
                    Item = insertData
                }, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            }

        }

        /// <summary>
        /// Create a relationship of the specied type
        /// </summary>
        /// <param name="relationshipType"></param>
        /// <param name="relationshipClassification"></param>
        /// <param name="sourceEntity"></param>
        /// <param name="targetEntity"></param>
        /// <returns></returns>
        private IdentifiedData CreateRelationship(Type relationshipType, Guid relationshipClassification, Guid? sourceEntity, Guid? targetEntity, Decimal? versionSequence)
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
                this.m_repository.Inserting -= this.PrePersistenceValidate;
                this.m_repository.Updating -= this.PrePersistenceValidate;
                this.m_repository.Inserted -= this.Inserted;
                this.m_repository.Updated -= this.Updated;
                this.m_repository.Retrieved -= this.Retrieved;
                this.m_repository.Obsoleting -= this.Obsoleting;
                this.m_repository.Querying -= this.Querying;
                this.m_repository.Queried -= this.Queried;
            }
        }

        /// <summary>
        /// Instructs the MDM service to merge the specified master with the linked duplicates
        /// </summary>
        /// <param name="master"></param>
        /// <param name="linkedDuplicates"></param>
        /// <returns></returns>
        public T Merge(T master, IEnumerable<T> linkedDuplicates)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instructs the MDM service to unmerge the specified master from unmerge duplicate
        /// </summary>
        /// <param name="master"></param>
        /// <param name="unmergeDuplicate"></param>
        /// <returns></returns>
        public T Unmerge(T master, T unmergeDuplicate)
        {
            throw new NotImplementedException();
        }
    }
}