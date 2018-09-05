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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Permissions;

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
        where T : IdentifiedData
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
        public MdmResourceListener()
        {
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            var taggable = e.Data as ITaggable;
            if (taggable?.Tags.Any(t => t.TagKey == "mdm.type" && t.TagKey == "M") == true &&
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

            // Find records for which this record could be match // async
            var relationshipType = identified is Entity ? typeof(EntityRelationship) : typeof(ActRelationship);
            var relationshipService = ApplicationContext.Current.GetService(typeof(IDataPersistenceService<>).MakeGenericType(relationshipType)) as IDataPersistenceService;
            var matchingRecords = matchService.Match(e.Data, this.m_resourceConfiguration.MatchConfiguration);

            // Matching records can only match with MASTER records
            matchingRecords = matchingRecords.Where(o => (o.Record as ITaggable)?.Tags.Any(t => t.TagKey == "mdm.type" && t.Value == "M") == true);
            var matchGroups = matchingRecords
                .GroupBy(o => o.Classification)
                .ToDictionary(o => o.Key, o => o);
            
            // Inbound record *IS A MASTER*
            // MASTER records can be duplicates or detected duplicates of other MASTER records. For example, 
            // if an inbound LOCAL record JOHN SMITH, 1980-01-01 results in the creation of a new MASTER record JOHN SMITH, 1980-01-01
            // and that MASTER record matches an existing one on file
            if (mdmTag.Value == "M" &&
                (matchGroups.ContainsKey(RecordMatchClassification.Match) &&
                matchGroups[RecordMatchClassification.Match].Count() > 1 ||
                matchGroups.ContainsKey(RecordMatchClassification.Probable)))
            {
                // We want to remove all previous probable matches
                var query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                    .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"source={identified.Key}&relationshipType={MdmConstants.DuplicateRecordRelationship}") }) as Expression;
                int tr = 0;
                var rels = relationshipService.Query(query, 0, 100, out tr);
                foreach (var r in rels)
                    relationshipService.Obsolete(r);

                // Now we want to create a new relationship for the type
                var matchRelationships = matchingRecords.Where(o => o.Classification == RecordMatchClassification.Match || o.Classification == RecordMatchClassification.Probable)
                    .Select(m => m.Record)
                    .OfType<IdentifiedData>()
                    .Select(m => this.CreateRelationship(relationshipType, MdmConstants.DuplicateRecordRelationship, identified.Key, m.Key));
                foreach (var r in matchRelationships)
                    relationshipService.Insert(r);

                // Add to return value
                typeof(T).GetRuntimeProperty("Relationships").SetValue(e.Data, matchRelationships.ToList());
            }
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
            else if(mdmTag.Value == "L")
            {

                List<IdentifiedData> insertData = new List<IdentifiedData>();

                // There is exactly one match and it is set to automerge
                if(matchGroups[RecordMatchClassification.Match].Count() == 1 
                    && this.m_resourceConfiguration.AutoMerge)
                {
                    var master = matchGroups[RecordMatchClassification.Match].First().Record;
                    var dataService = ApplicationContext.Current.GetService<IDataPersistenceService<T>>() as IDataPersistenceService;
                    insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, identified.Key, master.Key));

                    // Grab the current list of local records and fold this new data into the master
                    var query = typeof(QueryExpressionParser).GetGenericMethod(nameof(QueryExpressionParser.BuildLinqExpression), new Type[] { relationshipType }, new Type[] { typeof(NameValueCollection) })
                       .Invoke(null, new object[] { NameValueCollection.ParseQueryString($"target={master.Key}&relationshipType={MdmConstants.MasterRecordRelationship}") }) as Expression;
                    int tr = 0;
                    var currentLocals = relationshipService.Query(query, 0, 100, out tr).OfType<ISimpleAssociation>().Select(o=>dataService.Get(o.SourceEntityKey.Value));
                    var updatedMaster = this.UpdateMasterRecord(master, currentLocals.OfType<T>().Union(new T[] { e.Data }).ToArray());
                    dataService.Update(master);
                }
                else
                {
                    // We want to create a new master for this record
                    var master = this.CreateMasterRecord((T)identified);
                    insertData.Add(master);
                    insertData.Add(this.CreateRelationship(relationshipType, MdmConstants.MasterRecordRelationship, identified.Key, master.Key));

                    // Now add each of the found masters to the probable list
                    insertData.AddRange(matchGroups[RecordMatchClassification.Match].Select(m => this.CreateRelationship(relationshipType, MdmConstants.DuplicateRecordRelationship, identified.Key, m.Record.Key)));
                }

                // Add probable records
                insertData.AddRange(matchGroups[RecordMatchClassification.Probable].Select(m => this.CreateRelationship(relationshipType, MdmConstants.DuplicateRecordRelationship, identified.Key, m.Record.Key)));

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
        private IdentifiedData CreateRelationship(Type relationshipType, Guid relationshipClassification, Guid? sourceEntity, Guid? targetEntity)
        {
            var relationship = Activator.CreateInstance(relationshipType, relationshipClassification, targetEntity) as IdentifiedData;
            relationship.Key = Guid.NewGuid();
            (relationship as ISimpleAssociation).SourceEntityKey = sourceEntity;
            return relationship;
        }

        /// <summary>
        /// Create a master record from the specified local records
        /// </summary>
        /// <param name="localRecords">The local records that are to be used to generate a master record</param>
        /// <returns>The created master record</returns>
        private T CreateMasterRecord(params T[] localRecords)
        {
            return this.UpdateMasterRecord(null, localRecords);
        }

        /// <summary>
        /// Updates the master record
        /// </summary>
        /// <param name="currentMaster">The current master record that should be updated</param>
        /// <param name="localRecords">The local records that should be used to formulate the update to the master</param>
        private T UpdateMasterRecord(T currentMaster, params T[] localRecords)
        {
            if (localRecords.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(localRecords), "Must contain at least one local record");

            // Current master doesn't exist
            if (currentMaster == null)
            {
                currentMaster = Activator.CreateInstance<T>();
                currentMaster.Key = Guid.NewGuid();
                (currentMaster as Entity)?.Tags.Add(new EntityTag("mdm.type", "M"));
                (currentMaster as Act)?.Tags.Add(new ActTag("mdm.type", "M"));
            }

            // Process local records and copy the field values if no semantically equivalent object exists in the master and if the current master is protected
            currentMaster.SemanticUpdate(localRecords.Where(o => !(o is ISecurable) || (o as ISecurable).Policies.Count == 0).ToArray());

            return currentMaster;
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