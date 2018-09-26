using SanteDB.Core.Model;
using Newtonsoft.Json;
using SanteDB.Core.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SanteDB.Core.Model.EntityLoader;
using System.Security.Principal;
using MARC.HI.EHRS.SVC.Core.Services.Policy;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;

namespace SanteDB.Persistence.MDM.Model
{
    /// <summary>
    /// Represents a master record of an entity
    /// </summary>
    public class EntityMaster<T> : Entity, IMdmMaster<T>
        where T : IdentifiedData, new()
    {

        // The constructed master
        private T m_master;
        // The master record
        private Entity m_masterRecord;
        // Local records
        private List<T> m_localRecords;

        /// <summary>
        /// Create entity master
        /// </summary>
        public EntityMaster() : base()
        {
            this.ClassConceptKey = MdmConstants.MasterRecordClassification;
            this.DeterminerConceptKey = MdmConstants.MasterRecordDeterminer;

            if (!typeof(Entity).IsAssignableFrom(typeof(T)))
                throw new ArgumentOutOfRangeException("T must be Entity or subtype of Entity");
        }

        /// <summary>
        /// Construct an entity master record
        /// </summary>
        public EntityMaster(Entity master) : this()
        {
            this.CopyObjectData(master);
            this.m_masterRecord = master;
        }

        /// <summary>
        /// Get the constructed master reord
        /// </summary>
        public T GetMaster(IPrincipal principal)
        {
            if (this.m_master == null)
            {
                this.m_master = new T();
                this.m_master.CopyObjectData<IdentifiedData>(this.m_masterRecord);

                // Is there a relationship which is the record of truth
                var rot = this.LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordOfTruthRelationship);
                var pdp = ApplicationContext.Current.GetService<IPolicyDecisionService>();

                if (rot == null) // We have to create a synthetic record 
                {
                    this.m_master.SemanticCopy(this.LocalRecords.Where(o => pdp.GetPolicyDecision(principal, o).Outcome == PolicyDecisionOutcomeType.Grant).ToArray());
                }
                else // there is a ROT so use it to override the values
                {
                    this.m_master.SemanticCopy(rot.LoadProperty<T>("TargetEntity"));
                    this.m_master.SemanticCopyNullFields(this.LocalRecords.Where(o => pdp.GetPolicyDecision(principal, o).Outcome == PolicyDecisionOutcomeType.Grant).ToArray());
                }
                (this.m_master as Entity).Policies = this.LocalRecords.SelectMany(o => (o as Entity).Policies).Select(o=>new SecurityPolicyInstance(o.Policy, (PolicyGrantType)(int)pdp.GetPolicyOutcome(principal, o.Policy.Oid))).Where(o => o.GrantType == PolicyGrantType.Grant || o.Policy.CanOverride).ToList();
                (this.m_master as Entity).Tags.RemoveAll(o => o.TagKey == "mdm.type");
                (this.m_master as Entity).Tags.Add(new EntityTag("mdm.type", "M"));
            }
            return this.m_master;
        }

        /// <summary>
        /// Get the local records of this master
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public List<T> LocalRecords
        {
            get
            {
                if(this.m_localRecords == null)
                    this.m_localRecords = EntitySource.Current.Provider.Query<EntityRelationship>(o=>o.TargetEntityKey == this.Key && o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship).Select(o => o.LoadProperty<T>("SourceEntity")).OfType<T>().ToList();
                return this.m_localRecords;
            }
        }
    }
}
