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
 * Date: 2022-9-7
 */
using SanteDB.Core.Model;
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence.Collections
{
    /// <summary>
    /// Persistence tests for bundles
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class BundlePersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Tests the insertion or update of objects in the classic manner in which SanteDB
        /// and OpenIZ support processing of bundle contents (insert or update behavior)
        /// </summary>
        [Test]
        public void TestBatchOperationAuto()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var dadGuid = Guid.NewGuid();
                var bundle = new Bundle()
                {
                    Item = new List<Core.Model.IdentifiedData>()
                    {
                        new Person()
                        {
                            Key = dadGuid,
                            Names = new List<EntityName>() { new EntityName(NameUseKeys.Legal, "BundleSmith", "Johnathon" )},
                        },
                        new Patient()
                        {
                            Names = new List<EntityName>() { new EntityName(NameUseKeys.Legal, "BundleJones", "Sarah") },
                            Relationships = new List<EntityRelationship>()
                            {
                                new EntityRelationship(EntityRelationshipTypeKeys.Father, dadGuid)
                            }
                        }
                    }
                };

                var bundlePersistence = ApplicationServiceContext.Current.GetService<IRepositoryService<Bundle>>();
                var afterInsert = bundlePersistence.Insert(bundle);
                Assert.IsNotNull(afterInsert.Item[0].Key);
                Assert.IsNotNull(afterInsert.Item[1].Key);
                Assert.AreEqual(bundle.Item[0].Key, afterInsert.Item[0].Key); // No reorg
                Assert.IsNotNull((afterInsert.Item[0] as IVersionedData).VersionKey);
                Assert.IsNotNull((afterInsert.Item[1] as IVersionedData).VersionKey);
                Assert.AreNotEqual(bundle.Item[0], afterInsert.Item[0]); // New objects returned (from persistence layer)
                Assert.AreNotEqual(bundle.Item[1], afterInsert.Item[1]);
                Assert.AreEqual(BatchOperationType.Insert, afterInsert.Item[0].BatchOperation); // was inserted
                Assert.AreEqual(BatchOperationType.Insert, afterInsert.Item[1].BatchOperation); // was inserted

                // Validate persisted
                base.TestQuery<Person>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "BundleSmith")), 1);
                base.TestQuery<Patient>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "BundleJones")), 1);

                // New bundle - update patient and insert act
                bundle = new Bundle()
                {
                    Item = new List<Core.Model.IdentifiedData>()
                    {
                        new Act()
                        {
                            ClassConceptKey = ActClassKeys.Registration,
                            MoodConceptKey = MoodConceptKeys.Eventoccurrence,
                            Participations = new List<ActParticipation>()
                            {
                                new ActParticipation(ActParticipationKeys.RecordTarget, afterInsert.Item[1].Key)
                            },
                            ActTime = DateTimeOffset.Now
                        } ,
                        new Patient()
                        {
                            BatchOperation = BatchOperationType.Auto,
                            Key = afterInsert.Item[1].Key,
                            Names = new List<EntityName>() { new EntityName(NameUseKeys.Legal, "BundleJonesy", "Sara") },
                            Relationships = new List<EntityRelationship>() { new EntityRelationship(EntityRelationshipTypeKeys.Father, dadGuid) }
                        }
                    }
                };

                afterInsert = bundlePersistence.Insert(bundle);

                // Act depends on patient so patient should come first
                Assert.IsInstanceOf<Patient>(afterInsert.Item[0]);
                // Patient was updated 
                Assert.AreEqual(BatchOperationType.Update, afterInsert.Item[0].BatchOperation);
                Assert.AreEqual("BundleJonesy", (afterInsert.Item[0] as Entity).LoadProperty(o => o.Names).First().LoadProperty(o => o.Component).First().Value);

                // Ensure act is inserted
                Assert.AreEqual(BatchOperationType.Insert, afterInsert.Item[1].BatchOperation);
                Assert.IsNotNull(afterInsert.Item[1].Key);
                Assert.IsNotNull((afterInsert.Item[1] as IVersionedData).VersionKey);

                base.TestQuery<Patient>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "BundleJones")), 0);
                base.TestQuery<Patient>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "BundleJonesey")), 0);

            }
        }

        /// <summary>
        /// Test the insert/update/delete of objects from a mixed mode bundle which contains the appropriate 
        /// batch operation instructions
        /// </summary>
        [Test]
        public void TestMixedBatchMode()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var org1Guid = Guid.NewGuid();
                var bundle = new Bundle()
                {
                    Item = new List<Core.Model.IdentifiedData>()
                    {
                        new Organization()
                        {
                            BatchOperation = BatchOperationType.Insert,
                            Key = org1Guid,
                            Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, "Bundle Good Health Systems")},
                        },
                        new Organization()
                        {
                            BatchOperation = BatchOperationType.Insert,
                            Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, "Bundle Good Health Systems Technology") },
                            Relationships = new List<EntityRelationship>()
                            {
                                new EntityRelationship(EntityRelationshipTypeKeys.Parent, org1Guid)
                            }
                        },
                        new Organization()
                        {
                            BatchOperation = BatchOperationType.Insert,
                            Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, "Bundle Good Health Systems Logistics") },
                            Relationships = new List<EntityRelationship>()
                            {
                                new EntityRelationship(EntityRelationshipTypeKeys.Parent, org1Guid)
                            }
                        }
                    }
                };

                var bundlePersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>();
                var afterInsert = bundlePersistence.Insert(bundle, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                base.TestQuery<Organization>(o => o.Names.Any(n => n.Component.Any(c => c.Value.StartsWith("Bundle Good Health"))), 3);
                base.TestQuery<Organization>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Bundle Good Health Systems Technology")), 1);
                base.TestQuery<Organization>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Bundle Good Health Systems Logistics")), 1);
                // 2 children
                base.TestQuery<Organization>(o => o.Relationships.Any(r => r.RelationshipTypeKey == EntityRelationshipTypeKeys.Parent && r.TargetEntity.Names.Any(n => n.Component.Any(c => c.Value == "Bundle Good Health Systems"))), 2);

                // Now we want to instruct a bundle for updating an existing object and removing logistics division
                bundle = new Bundle()
                {
                    Item = new List<IdentifiedData>() {
                        new Organization()
                        {
                            Key = afterInsert.Item[1].Key,
                            BatchOperation = BatchOperationType.Update,
                            Names = new List<EntityName>() { new EntityName(NameUseKeys.OfficialRecord, "Bundle Good Health Systems Technology and Logistics") }
                        },
                        new Organization()
                        {
                            Key = afterInsert.Item[2].Key,
                            BatchOperation = BatchOperationType.Delete
                        }
                    }
                };
                afterInsert = bundlePersistence.Insert(bundle, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                Assert.AreEqual(BatchOperationType.Update, afterInsert.Item[0].BatchOperation);
                Assert.AreEqual(BatchOperationType.Delete, afterInsert.Item[1].BatchOperation);
                //Assert.AreEqual(StatusKeys.Purged, (afterInsert.Item[1] as IHasState).StatusConceptKey);
                base.TestQuery<Organization>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Bundle Good Health Systems Technology")), 0);
                base.TestQuery<Organization>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Bundle Good Health Systems Logistics")), 0);
                base.TestQuery<Organization>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Bundle Good Health Systems Technology and Logistics")), 1);
                base.TestQuery<Organization>(o => o.Relationships.Any(r=>r.RelationshipTypeKey == EntityRelationshipTypeKeys.Parent && r.TargetEntity.Names.Any(n => n.Component.Any(c => c.Value == "Bundle Good Health Systems"))), 1);

                // Delete the relationship only
                bundle = new Bundle()
                {
                    Item = new List<IdentifiedData>()
                    {
                        new EntityRelationship()
                        {
                            Key = (afterInsert.Item[0] as Organization).LoadProperty(o=>o.Relationships).First().Key,
                            BatchOperation = BatchOperationType.Delete
                        }
                    }
                };
                afterInsert = bundlePersistence.Insert(bundle, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                Assert.IsInstanceOf<EntityRelationship>(afterInsert.Item.First());
                Assert.IsNotNull((afterInsert.Item.First() as EntityRelationship).ObsoleteVersionSequenceId);
            }
        }

    }
}
