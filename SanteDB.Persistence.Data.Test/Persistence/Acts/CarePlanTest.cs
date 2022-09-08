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
using NUnit.Framework;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// Tests for care plan persistence
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class CarePlanTest : DataPersistenceTest
    {

        /// <summary>
        /// Tests the care plan persistence service
        /// </summary>
        [Test]
        public void TestWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                Protocol protocol1 = base.TestInsert(new Protocol()
                {
                    Definition = new byte[] { 1, 2, 3, 4, 5 },
                    HandlerClass = typeof(String),
                    Name = "Sample Care Plan Protocol 1",
                    Oid = "2.25.400494949494949944"
                }), protocol2 = base.TestInsert(new Protocol()
                {
                    Definition = new byte[] { 1, 2, 3, 4, 5 },
                    HandlerClass = typeof(String),
                    Name = "Sample Care Plan Protocol 2",
                    Oid = "2.25.9483443938383"
                });

                var cp = new CarePlan()
                {
                    Protocols = new List<ActProtocol>()
                    {
                        new ActProtocol()
                        {
                            ProtocolKey = protocol1.Key
                        },
                        new ActProtocol()
                        {
                            ProtocolKey = protocol2.Key
                        }
                    },
                    StopTime = new DateTimeOffset(2025, 10, 10, 0, 0, 0, new TimeSpan(-5, 0, 0)),
                    Participations = new List<ActParticipation>()
                    {
                        new ActParticipation(ActParticipationKeys.RecordTarget, new Patient() {
                            Names = new List<Core.Model.Entities.EntityName>()
                            {
                                new Core.Model.Entities.EntityName(NameUseKeys.Legal, "Smith", "Robert")
                            },
                            Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                            {
                                new Core.Model.DataTypes.EntityIdentifier(new IdentityDomain("CP_TEST", "Careplan test", "2.25.4939393933"), "123-CP-1")
                            }
                        })
                    },
                    Relationships = new List<ActRelationship>()
                    {
                        new ActRelationship(ActRelationshipTypeKeys.HasComponent, new Act() {
                            StartTime = DateTimeOffset.Now,
                            StopTime = DateTimeOffset.Now.AddMonths(10),
                            MoodConceptKey = ActMoodKeys.Propose,
                            ClassConceptKey = ActClassKeys.Act,
                            IsNegated = false,
                            Protocols = new List<ActProtocol>()
                            {
                                new ActProtocol()
                                {
                                    ProtocolKey = protocol1.Key,
                                    Sequence = 1
                                }
                            }
                        }),
                        new ActRelationship(ActRelationshipTypeKeys.HasComponent, new Act() {
                            StartTime = DateTimeOffset.Now.AddMonths(10),
                            StopTime = DateTimeOffset.Now.AddMonths(20),
                            MoodConceptKey = ActMoodKeys.Propose,
                            ClassConceptKey = ActClassKeys.Act,
                            IsNegated = false,
                            Protocols = new List<ActProtocol>()
                            {
                                new ActProtocol()
                                {
                                    ProtocolKey = protocol1.Key,
                                    Sequence = 2
                                }
                            }
                        }),
                        new ActRelationship(ActRelationshipTypeKeys.HasComponent, new Act() {
                            StartTime = DateTimeOffset.Now,
                            StopTime = DateTimeOffset.Now.AddMonths(4),
                            MoodConceptKey = ActMoodKeys.Propose,
                            ClassConceptKey = ActClassKeys.Act,
                            IsNegated = false,
                            Protocols = new List<ActProtocol>()
                            {
                                new ActProtocol()
                                {
                                    ProtocolKey = protocol2.Key,
                                    Sequence = 1
                                }
                            }
                        })
                    }
                };

                var afterInsert = base.TestInsert(cp);
                Assert.IsInstanceOf<CarePlan>(afterInsert);

                // Test query
                // By patient
                base.TestQuery<CarePlan>(o => o.Participations.Where(p => p.ParticipationRoleKey == ActParticipationKeys.RecordTarget).Any(p => p.PlayerEntity.Identifiers.Any(i => i.Value == "123-CP-1")), 1);
                base.TestQuery<CarePlan>(o => o.Participations.Where(p => p.ParticipationRoleKey == ActParticipationKeys.RecordTarget).Any(p => p.PlayerEntity.Identifiers.Any(i => i.Value == "123-CP-2")), 0);

                // By sub-act status
                base.TestQuery<CarePlan>(o => o.Relationships.Where(p => p.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent).Any(p => p.TargetAct.StatusConceptKey == StatusKeys.New), 1);
                base.TestQuery<CarePlan>(o => o.Relationships.Where(p => p.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent).Any(p => p.TargetAct.StatusConceptKey == StatusKeys.Completed), 0);

                // By protocol 
                base.TestQuery<CarePlan>(o => o.Protocols.Any(p => p.Protocol.Oid == protocol1.Oid), 1);
                base.TestQuery<CarePlan>(o => o.Relationships.Any(r => r.TargetAct.Protocols.Any(p => p.Protocol.Oid == protocol1.Oid) && r.TargetAct.MoodConceptKey == ActMoodKeys.Propose), 1);
                // Test loading
                var afterQuery = base.TestQuery<CarePlan>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.Protocols).Count);
                Assert.AreEqual(3, afterQuery.LoadProperty(o => o.Relationships).Count);

                var afterUpdate = base.TestUpdate(afterInsert, o =>
                {
                    // Now we want to shift the dates and cancel one of the actions
                    var fulfillment = base.TestInsert(new Act()
                    {
                        ActTime = DateTimeOffset.Now.AddMonths(8),
                        MoodConceptKey = ActMoodKeys.Eventoccurrence,
                        ClassConceptKey = ActClassKeys.Act,
                        IsNegated = false,
                        Protocols = new List<ActProtocol>()
                        {
                            new ActProtocol()
                            {
                                ProtocolKey = protocol1.Key,
                                Sequence = 1
                            }
                        },
                        Relationships = new List<ActRelationship>()
                        {
                            new ActRelationship(ActRelationshipTypeKeys.Fulfills, o.LoadProperty(r=>r.Relationships).First().TargetActKey)
                        }
                    });

                    // Next we have to update the fulfilled act to be complete
                    base.TestUpdate(o.Relationships.First().LoadProperty(r => r.TargetAct), (t) =>
                      {
                          t.StatusConceptKey = StatusKeys.Completed;
                          return t;
                      });

                    o.LoadProperty(p => p.Protocols).RemoveAt(1);
                    o.Relationships.RemoveAt(2);
                    return o;
                });

                // By sub-act status
                base.TestQuery<CarePlan>(o => o.Relationships.Where(p => p.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent).Any(p => p.TargetAct.StatusConceptKey == StatusKeys.New), 1);
                base.TestQuery<CarePlan>(o => o.Relationships.Where(p => p.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent).Any(p => p.TargetAct.StatusConceptKey == StatusKeys.Completed), 1);

                // Test with entity persistence
                afterQuery = base.TestQuery<CarePlan>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Protocols).Count);
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.Relationships).Count);

            }
        }
    }
}
