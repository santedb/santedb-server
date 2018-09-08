﻿using Newtonsoft.Json;
using SanteDB.Core.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SanteDB.Core.Model;
using SanteDB.Core.Model.EntityLoader;
using System.Security.Principal;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services.Policy;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Constants;

namespace SanteDB.Persistence.MDM.Model
{
    /// <summary>
    /// Represents the master record of an act
    /// </summary>
    public class ActMaster<T> : Act, IMdmMaster<T>
        where T : IdentifiedData, new()
    {

        // The constructed master
        private T m_master;
        // The master record
        private Act m_masterRecord;
        // Local records
        private List<T> m_localRecords;

        /// <summary>
        /// Create entity master
        /// </summary>
        public ActMaster() : base()
        {
            this.ClassConceptKey = MdmConstants.MasterRecordClassification;
            this.MoodConceptKey = MdmConstants.MasterRecordDeterminer;
            if (!typeof(Act).IsAssignableFrom(typeof(T)))
                throw new ArgumentOutOfRangeException("T must be Entity or subtype of Entity");
        }

        /// <summary>
        /// Construct an entity master record
        /// </summary>
        public ActMaster(Act master) : this()
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
                // Is there a relationship which is the record of truth
                var rot = this.LoadCollection<ActRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordOfTruthRelationship);
                if (rot == null) // We have to create a synthetic record 
                {
                    var pdp = ApplicationContext.Current.GetService<IPolicyDecisionService>();
                    this.m_master = new T();
                    this.m_master.SemanticCopy(this.LocalRecords.Where(o => pdp.GetPolicyDecision(principal, o).Outcome == PolicyDecisionOutcomeType.Grant).ToArray());
                    this.m_master.CopyObjectData<IdentifiedData>(this.m_masterRecord);
                    (this.m_master as Act).Tags.RemoveAll(o => o.TagKey == "mdm.type");
                    (this.m_master as Act).Tags.Add(new ActTag("mdm.type", "M"));
                }
                else
                    this.m_master = rot.LoadProperty<T>("TargetAct");
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
                if (this.m_localRecords == null)
                    this.m_localRecords = EntitySource.Current.Provider.Query<ActRelationship>(o => o.TargetActKey == this.Key && o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship).Select(o => o.LoadProperty<T>("SourceEntity")).OfType<T>().ToList();
                return this.m_localRecords;
            }
        }
    }
}
