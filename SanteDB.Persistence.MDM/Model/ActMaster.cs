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
using Newtonsoft.Json;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Xml.Serialization;

namespace SanteDB.Persistence.MDM.Model
{
    /// <summary>
    /// Represents the master record of an act
    /// </summary>
    public class ActMaster<T> : Act, IMdmMaster<T>
        where T : IdentifiedData, new()
    {
        /// <summary>
        /// Get the type name
        /// </summary>
        public override string Type { get => $"{typeof(T).Name}Master"; set { } }

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
            var master = new T();
            master.CopyObjectData<IdentifiedData>(this.m_masterRecord);

            // Is there a relationship which is the record of truth
            var rot = this.LoadCollection<ActRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordOfTruthRelationship);
            var pdp = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();
            var locals = this.LocalRecords.Where(o => {
                if (pdp.GetPolicyDecision(principal, o).Outcome == PolicyGrantType.Grant)
                    return true;
                else
                {
                    AuditUtil.AuditMasking(o, true);
                    return false;
                }
            }).ToArray();
            
            if (locals.Length == 0) // Not a single local can be viewed
                return null;
            else if (rot == null) // We have to create a synthetic record 
            {
                master.SemanticCopy(locals);
            }
            else // there is a ROT so use it to override the values
            {
                master.SemanticCopy(rot.LoadProperty<T>("TargetAct"));
                master.SemanticCopyNullFields(locals);
            }
                (master as Act).Policies = this.LocalRecords.SelectMany(o => (o as Act).Policies).Where(o => o.Policy.CanOverride).ToList();
            (master as Act).Tags.RemoveAll(o => o.TagKey == "mdm.type");
            (master as Act).Tags.Add(new ActTag("mdm.type", "M"));
            return master;
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
