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
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Xml.Serialization;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Model.Constants;

namespace SanteDB.Persistence.MDM.Model
{
    /// <summary>
    /// Represents a master record of an entity
    /// </summary>
    public class EntityMaster<T> : Entity, IMdmMaster<T>
        where T : IdentifiedData, new()
    {

        /// <summary>
        /// Get the type name
        /// </summary>
        public override string Type { get => $"{typeof(T).Name}Master"; set { } }

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
            var master = new T();
            var entityMaster = master as Entity;
            master.CopyObjectData<IdentifiedData>(this.m_masterRecord, overwritePopulatedWithNull: false, ignoreTypeMismatch: true);

            // Is there a relationship which is the record of truth
            var rot = this.LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == MdmConstants.MasterRecordOfTruthRelationship);
            var pdp = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();
            var locals = this.LocalRecords.Where(o => {
                if (pdp.GetPolicyDecision(principal, o).Outcome == PolicyGrantType.Grant)
                {
                    return true;
                }
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
                master.SemanticCopy(rot.LoadProperty<T>("TargetEntity"));
                master.SemanticCopyNullFields(locals);
            }

            entityMaster.Policies = this.LocalRecords.SelectMany(o => (o as Entity).Policies).Select(o => new SecurityPolicyInstance(o.Policy, (PolicyGrantType)(int)pdp.GetPolicyOutcome(principal, o.Policy.Oid))).Where(o => o.GrantType == PolicyGrantType.Grant || o.Policy.CanOverride).ToList();
            entityMaster.Tags.RemoveAll(o => o.TagKey == "mdm.type");
            entityMaster.Tags.Add(new EntityTag("mdm.type", "M"));
            entityMaster.Tags.Add(new EntityTag("$mdm.resource", typeof(T).Name));
            entityMaster.Tags.Add(new EntityTag("$alt.keys", String.Join(";", this.m_localRecords.Select(o => o.Key.ToString()))));
           
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
                {
                    this.m_localRecords = EntitySource.Current.Provider.Query<EntityRelationship>(o => o.TargetEntityKey == this.Key && o.RelationshipTypeKey == MdmConstants.MasterRecordRelationship).Select(o => o.LoadProperty<T>("SourceEntity")).OfType<T>().ToList();
                    this.m_localRecords.OfType<Entity>().ToList().ForEach(o =>
                    {
                        o.LoadCollection<EntityRelationship>(nameof(Entity.Relationships));
                        o.LoadCollection<EntityAddress>(nameof(Entity.Addresses));
                        o.LoadCollection<EntityTag>(nameof(Entity.Tag));
                        o.LoadCollection<EntityTelecomAddress>(nameof(Entity.Telecoms));
                        o.LoadCollection<EntityIdentifier>(nameof(Entity.Identifiers));
                        o.LoadCollection<EntityName>(nameof(Entity.Names));
                        o.LoadCollection<EntityNote>(nameof(Entity.Notes));
                        o.LoadCollection<SecurityPolicyInstance>(nameof(Entity.Policies));
                        o.LoadCollection<EntityExtension>(nameof(Entity.Extensions));

                        if (o is Person)
                        {
                            (o as Person).LoadCollection<PersonLanguageCommunication>(nameof(Person.LanguageCommunication));
                            var family = EntitySource.Current.Provider.Query<EntityRelationship>(r => r.TargetEntityKey == o.Key && o.TypeConcept.ConceptSets.Where(cs => cs.Mnemonic == "FamilyMember").Any()).ToList();
                            o.Relationships.AddRange(family);
                        }
                        if (o is Place)
                            (o as Place).LoadCollection<PlaceService>(nameof(Place.Services));

                        // Correct to master
                        o.Relationships.ForEach(r =>
                        {
                            if (r.SourceEntityKey == o.Key)
                                r.SourceEntityKey = this.m_masterRecord.Key;
                            else if (r.TargetEntityKey == o.Key)
                                r.TargetEntityKey = this.m_masterRecord.Key;
                        });
                    });
                }
                return this.m_localRecords;
            }
        }
    }
}
