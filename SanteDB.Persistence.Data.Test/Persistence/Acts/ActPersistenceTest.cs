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
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core;
using SanteDB.Core.Services;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// Test the persistence of acts
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class ActPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test the insert and query back of a basic 
        /// </summary>
        [Test]
        public void TestInsertBasicAct()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var act = new Act()
                {
                    ActTime = DateTime.Now,
                    ClassConceptKey = ActClassKeys.Battery,
                    TypeConceptKey = ObservationTypeKeys.ClinicalState,
                    IsNegated = true,
                    MoodConceptKey = MoodConceptKeys.Goal,
                    ReasonConceptKey = ActReasonKeys.Broken
                };

                // Test insert
                var afterInsert = base.TestInsert(act);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(StatusKeys.New, afterInsert.StatusConceptKey);
                Assert.AreEqual(ActClassKeys.Battery, afterInsert.ClassConceptKey);
                Assert.AreEqual(ObservationTypeKeys.ClinicalState, afterInsert.TypeConceptKey);
                Assert.AreEqual(ActReasonKeys.Broken, afterInsert.ReasonConceptKey);
                Assert.IsTrue(afterInsert.IsNegated);

                // Test fetch
                var afterQuery = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual(StatusKeys.New, afterQuery.StatusConceptKey);
                Assert.AreEqual(ActClassKeys.Battery, afterQuery.ClassConceptKey);
                Assert.AreEqual(ObservationTypeKeys.ClinicalState, afterQuery.TypeConceptKey);
                Assert.AreEqual(ActReasonKeys.Broken, afterQuery.ReasonConceptKey);
                Assert.IsTrue(afterQuery.IsNegated);

            }
        }

        /// <summary>
        /// Test the insertion of an act with template defined
        /// </summary>
        [Test]
        public void TestInsertActTemplates()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var act = new Act()
                {
                    ActTime = DateTime.Now,
                    ClassConceptKey = ActClassKeys.Condition,
                    IsNegated = false,
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    Protocols = new List<ActProtocol>()
                    {
                        new ActProtocol()
                        {
                            Sequence = 1,
                            Protocol = new Protocol()
                            {
                                Definition = new byte[] {1, 2, 3, 4},
                                HandlerClass = typeof(String),
                                Name = "Some Name This Is",
                                Oid = "2.25.40430439493043"
                            },
                            StateData = new byte[] { 1,2,3,4 }
                        }
                    },
                    ReasonConceptKey = ActReasonKeys.ProfessionalJudgement,
                    StatusConceptKey = StatusKeys.Active,
                    TypeConceptKey = ObservationTypeKeys.Problem,
                    Template = new Core.Model.DataTypes.TemplateDefinition()
                    {
                        Description = "A sample template",
                        Mnemonic = "act.sample",
                        Name = "ThisSample",
                        Oid = "2.25.4040494"
                    }
                };

                var afterInsert = base.TestInsert(act);
                Assert.AreEqual(1, afterInsert.Protocols.Count);
                Assert.AreEqual("act.sample", afterInsert.LoadProperty(o => o.Template).Mnemonic);

                var afterQuery = base.TestQuery<Act>(o => o.Template.Mnemonic == "act.sample", 1).AsResultSet().First();
                Assert.AreEqual("act.sample", afterQuery.LoadProperty(o => o.Template).Mnemonic);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Protocols).Count);
                Assert.AreEqual(1, afterQuery.Protocols[0].Sequence);
                Assert.AreEqual("2.25.40430439493043", afterQuery.Protocols[0].LoadProperty(o => o.Protocol).Oid);
                base.TestQuery<Act>(o => o.Protocols.Any(p => p.Protocol.Oid == "2.25.40430439493043"), 1);

                // Try adding protocol
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.Template = new Core.Model.DataTypes.TemplateDefinition()
                    {
                        Description = "Another sample template",
                        Mnemonic = "act.sample2",
                        Name = "ThisSample2",
                        Oid = "2.25.4040494232"
                    };
                    o.TemplateKey = null;
                    o.Protocols.Add(new ActProtocol()
                    {
                        Sequence = 2,
                        Protocol = new Protocol()
                        {
                            Definition = Encoding.UTF8.GetBytes("TesT"),
                            Name = "This is a name",
                            Oid = "2.2.34343433",
                            HandlerClass = typeof(String)
                        }
                    });
                    return o;
                });
                Assert.AreEqual(2, afterUpdate.Protocols.Count);

                base.TestQuery<Act>(o => o.Template.Mnemonic == "act.sample", 0);
                base.TestQuery<Act>(o => o.Template.Mnemonic == "act.sample2", 1);
                afterQuery = base.TestQuery<Act>(o => o.Protocols.Any(p => p.Protocol.Oid == "2.2.34343433"), 1).AsResultSet().First();
                Assert.AreEqual("act.sample2", afterQuery.LoadProperty(o => o.Template).Mnemonic);
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.Protocols).Count);
                Assert.AreEqual(1, afterQuery.Protocols[0].Sequence);
                Assert.AreEqual(2, afterQuery.Protocols[1].Sequence);
                Assert.AreEqual("2.25.40430439493043", afterQuery.Protocols[0].LoadProperty(o => o.Protocol).Oid);
                Assert.AreEqual("2.2.34343433", afterQuery.Protocols[1].LoadProperty(o => o.Protocol).Oid);
            }
        }


        /// <summary>
        /// Test tag persistence
        /// </summary>
        [Test]
        public void TestTagPersistence()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var act = new Act()
                {
                    ActTime = DateTime.Now,
                    ClassConceptKey = ActClassKeys.Battery,
                    TypeConceptKey = ObservationTypeKeys.ClinicalState,
                    IsNegated = true,
                    MoodConceptKey = MoodConceptKeys.Goal,
                    ReasonConceptKey = ActReasonKeys.Broken
                };

                var afterInsert = base.TestInsert(act);
                Assert.IsNotNull(afterInsert.CreationTime);
                Assert.AreEqual(0, afterInsert.LoadProperty(o => o.Tags).Count);

                // Test fetch
                var fetched = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetched.First();
                Assert.AreEqual(0, afterFetch.LoadProperty(o => o.Tags).Count);

                // Add tags
                var tagService = ApplicationServiceContext.Current.GetService<ITagPersistenceService>();
                Assert.IsNotNull(tagService);
                tagService.Save(afterInsert.Key.Value, new ActTag("foo", "bars"));

                fetched = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Tags).Count);
                Assert.AreEqual("bars", afterFetch.LoadProperty(o => o.Tags).First().Value);

                // Update tag
                tagService.Save(afterInsert.Key.Value, new ActTag("foo", "baz"));
                fetched = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Tags).Count);
                Assert.AreEqual("baz", afterFetch.LoadProperty(o => o.Tags).First().Value);

                // Add tag
                tagService.Save(afterInsert.Key.Value, new ActTag("foobar", "baz"));
                fetched = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(2, afterFetch.LoadProperty(o => o.Tags).Count);

                // Remove tag
                tagService.Save(afterInsert.Key.Value, new ActTag("foobar", null));
                fetched = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                afterFetch = fetched.First();
                Assert.AreEqual(1, afterFetch.LoadProperty(o => o.Tags).Count);

                // Query by tag
                base.TestQuery<Act>(o => o.Tags.Any(t => t.Value == "baz"), 1);
                base.TestQuery<Act>(o => o.Tags.Any(t => t.Value == "bars"), 0);

            }
        }

        /// <summary>
        /// Tests the insertion of an act with all properties
        /// </summary>
        [Test]
        public void TestInsertActFull()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var act = new Act()
                {
                    Template = new TemplateDefinition()
                    {
                        Mnemonic = "act.concern.aefi",
                        Name = "Adverse Event Following Immunization",
                        Description = "Captures an adverse drug event which has occurred after administration of a immunization"
                    },
                    MoodConceptKey = MoodConceptKeys.Eventoccurrence,
                    ClassConceptKey = ActClassKeys.Condition,
                    TypeConceptKey = Guid.Parse("0744B6AD-BE39-4A08-B64D-F61CB8282267"),
                    StatusConceptKey = StatusKeys.Active,
                    StartTime = new DateTimeOffset(2021, 01, 01, 0, 0, 0, new TimeSpan(-5, 0, 0)),
                    Relationships = new List<ActRelationship>()
                    {
                        new ActRelationship(ActRelationshipTypeKeys.HasSubject, new CodedObservation()
                        {
                            MoodConceptKey = MoodConceptKeys.Eventoccurrence,
                            TypeConceptKey = IntoleranceObservationTypeKeys.DrugIntolerance,
                            ActTime = DateTime.Now,
                            StatusConceptKey = StatusKeys.Completed,
                            ValueKey = NullReasonKeys.Unknown
                        })
                    },
                    Participations = new List<ActParticipation>()
                    {
                        new ActParticipation(ActParticipationKeys.Location, new Place()
                        {
                            Names = new List<EntityName>()
                            {
                                new EntityName(NameUseKeys.Legal, "Good Health Hospital Testing Facility")
                            },
                            Addresses = new List<EntityAddress>()
                            {
                                new EntityAddress(AddressUseKeys.Direct, "123 Main Street North", "Beamsville", "ON","CA", "L0R2A0")
                            }
                        }),
                        new ActParticipation(ActParticipationKeys.Authororiginator, new Person()
                        {
                            Names = new List<EntityName>()
                            {
                                new EntityName(NameUseKeys.Legal, "Smith","Bob")
                            }
                        }),
                        new ActParticipation(ActParticipationKeys.Performer, new Person()
                        {
                            Names = new List<EntityName>()
                            {
                                new EntityName(NameUseKeys.Legal, "Jones","Allison")
                            }
                        })
                    },
                    Identifiers = new List<ActIdentifier>()
                    {
                        new ActIdentifier(this.GetOrCreateAA(), "123-303-TEST30")
                    },
                    Extensions = new List<ActExtension>()
                    {
                        new ActExtension(ExtensionTypeKeys.JpegPhotoExtension, new byte[] { 1, 2, 3, 4, 5, 6, 7 })
                    },
                    GeoTag = new GeoTag(20, 21.1, false),
                    IsNegated = false,
                    Notes = new List<ActNote>()
                    {
                        new ActNote()
                        {
                            Author= new Person()
                            {
                                Names = new List<EntityName>()
                                {
                                    new EntityName(NameUseKeys.Legal, "Smith","Bob")
                                }
                            },
                            Text = "The patient had hives appear after XXXXXX minutes of observation"
                        }
                    },
                    Protocols = new List<ActProtocol>()
                    {
                        new ActProtocol()
                        {
                            Protocol = new Protocol()
                            {
                                Definition = new byte[] { 1,2,3,4,5,6},
                                HandlerClass = typeof(String),
                                Name = "Allergy Testing Protocol",
                                Oid = "2.25.404034939439433"
                            },
                            Sequence = 1,
                            StateData = new byte[] { 2,3,4,5,6,7}
                        }
                    },
                    ReasonConceptKey = ActReasonKeys.ProfessionalJudgement
                };


                var afterInsert = base.TestInsert(act);

                // Test queries on acts
                base.TestQuery<Act>(o => o.ReasonConceptKey == ActReasonKeys.ProfessionalJudgement && o.Relationships.Where(r => r.RelationshipTypeKey == ActRelationshipTypeKeys.HasSubject).Any(a => a.TargetAct.TypeConceptKey == IntoleranceObservationTypeKeys.DrugIntolerance), 1);
                base.TestQuery<Act>(o => o.ReasonConceptKey == ActReasonKeys.ProfessionalJudgement && o.Relationships.Where(r => r.RelationshipTypeKey == ActRelationshipTypeKeys.HasSubject).Any(a => a.TargetAct.TypeConceptKey == IntoleranceObservationTypeKeys.FoodIntolerance), 0);
                base.TestQuery<Act>(o => o.Participations.Where(g => g.ParticipationRoleKey == ActParticipationKeys.Location).Any(l => l.PlayerEntity.Names.Any(n => n.Component.Any(c => c.Value == "Good Health Hospital Testing Facility"))), 1);
                base.TestQuery<Act>(o => o.Participations.Where(g => g.ParticipationRoleKey == ActParticipationKeys.Performer).Any(l => l.PlayerEntity.Names.Any(n => n.Component.Any(c => c.Value == "Good Health Hospital Testing Facility"))), 0);
                base.TestQuery<Act>(o => o.Participations.Where(g => g.ParticipationRoleKey == ActParticipationKeys.Performer).Any(l => l.PlayerEntity.Names.Any(n => n.Component.Any(c => c.Value == "Jones"))), 1);
                base.TestQuery<Act>(o => o.Protocols.Any(p => p.Protocol.Oid == "2.25.404034939439433"), 1);

                // Test loading properties
                var afterQuery = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual(MoodConceptKeys.Eventoccurrence, afterQuery.MoodConceptKey);
                Assert.AreEqual(ActClassKeys.Condition, afterQuery.ClassConceptKey);
                Assert.AreEqual(ActReasonKeys.ProfessionalJudgement, afterQuery.ReasonConceptKey);
                Assert.AreEqual(StatusKeys.Active, afterQuery.StatusConceptKey);
                Assert.AreEqual(new DateTimeOffset(2021, 01, 01, 0, 0, 0, new TimeSpan(-5, 0, 0)), afterQuery.StartTime);
                Assert.IsNull(afterQuery.StopTime);
                Assert.IsNull(afterQuery.ActTime);

                // Delay load properties
                Assert.AreEqual("act.concern.aefi", afterQuery.LoadProperty(o => o.Template).Mnemonic);
                Assert.AreEqual("2.25.404034939439433", afterQuery.LoadProperty(o => o.Protocols).First().LoadProperty(o => o.Protocol).Oid);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Relationships).Count);
                Assert.AreEqual(ActRelationshipTypeKeys.HasSubject, afterQuery.Relationships[0].RelationshipTypeKey);
                Assert.AreEqual("HasSubject", afterQuery.Relationships[0].LoadProperty(o => o.RelationshipType).Mnemonic);
                Assert.AreEqual(IntoleranceObservationTypeKeys.DrugIntolerance, afterQuery.Relationships[0].LoadProperty(o => o.TargetAct).TypeConceptKey);
                Assert.AreEqual(3, afterQuery.LoadProperty(o => o.Participations).Count);
                Assert.AreEqual(ActParticipationKeys.Location, afterQuery.Participations[0].ParticipationRoleKey);
                Assert.AreEqual(ActParticipationKeys.Authororiginator, afterQuery.Participations[1].ParticipationRoleKey);
                Assert.AreEqual(ActParticipationKeys.Performer, afterQuery.Participations[2].ParticipationRoleKey);
                Assert.AreEqual("Good Health Hospital Testing Facility", afterQuery.Participations[0].LoadProperty(o => o.PlayerEntity).LoadProperty(o => o.Names).First().LoadProperty(o => o.Component).First().Value);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Identifiers).Count);
                Assert.AreEqual("123-303-TEST30", afterQuery.Identifiers.First().Value);
                Assert.AreEqual(20, afterQuery.LoadProperty(o => o.GeoTag).Lat);
                Assert.AreEqual(ExtensionTypeKeys.JpegPhotoExtension, afterQuery.LoadProperty(o => o.Extensions).First().ExtensionTypeKey);
                Assert.AreEqual("The patient had hives appear after XXXXXX minutes of observation", afterQuery.LoadProperty(o => o.Notes).First().Text);
                Assert.AreEqual("Allergy Testing Protocol", afterQuery.LoadProperty(o => o.Protocols).First().LoadProperty(o => o.Protocol).Name);
                Assert.IsFalse(afterQuery.IsNegated);

            }
        }

        /// <summary>
        /// Get or create AA
        /// </summary>
        private Guid GetOrCreateAA()
        {
            var aaService = ApplicationServiceContext.Current.GetService<IIdentityDomainRepositoryService>();
            var aa = aaService.Get("HIN");
            if (aa == null)
            {
                aa = aaService.Insert(new IdentityDomain("HIN", "Health Identification Number", "2.25.303030") { Url = "http://hin.test" });
            }
            return aa.Key.Value;
        }

        /// <summary>
        /// Tests updating a full act 
        /// </summary>
        [Test]
        public void TestUpdateAct()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var act = new Act()
                {
                    Template = new TemplateDefinition()
                    {
                        Mnemonic = "act.concern.aefi2",
                        Name = "Adverse Event Following Immunization",
                        Description = "Captures an adverse drug event which has occurred after administration of a immunization"
                    },
                    MoodConceptKey = MoodConceptKeys.Eventoccurrence,
                    ClassConceptKey = ActClassKeys.Condition,
                    TypeConceptKey = Guid.Parse("0744B6AD-BE39-4A08-B64D-F61CB8282267"),
                    StatusConceptKey = StatusKeys.Active,
                    StartTime = new DateTimeOffset(2021, 01, 01, 0, 0, 0, new TimeSpan(-5, 0, 0)),
                    Participations = new List<ActParticipation>()
                    {
                        new ActParticipation(ActParticipationKeys.Location, new Place()
                        {
                            Names = new List<EntityName>()
                            {
                                new EntityName(NameUseKeys.Legal, "Good Health Hospital Testing Facility 2")
                            }
                        })
                    },
                    Identifiers = new List<ActIdentifier>()
                    {
                        new ActIdentifier(this.GetOrCreateAA(), "321-313-TEST30")
                    },
                    Extensions = new List<ActExtension>()
                    {
                        new ActExtension(ExtensionTypeKeys.JpegPhotoExtension, new byte[] { 1, 2, 3, 4, 5, 6, 7 })
                    },
                    IsNegated = false,
                    ReasonConceptKey = ActReasonKeys.ProfessionalJudgement
                };


                var afterInsert = base.TestInsert(act);

                // Test queries on acts
                var afterQuery = base.TestQuery<Act>(o => o.Participations.Where(g => g.ParticipationRoleKey == ActParticipationKeys.Location).Any(l => l.PlayerEntity.Names.Any(n => n.Component.Any(c => c.Value == "Good Health Hospital Testing Facility 2"))), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Participations).Count);

                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.LoadProperty(x => x.Participations).Add(new ActParticipation(ActParticipationKeys.Authororiginator, new Person()
                    {
                        Names = new List<EntityName>()
                        {
                            new EntityName(NameUseKeys.Legal, "Smith", "Bob")
                        }
                    }));
                    o.LoadProperty(x => x.Participations).Add(new ActParticipation(ActParticipationKeys.Performer, new Person()
                    {
                        Names = new List<EntityName>()
                            {
                                new EntityName(NameUseKeys.Legal, "Jones","Allison")
                            }
                    }));
                    return o;
                });

                // Re-test
                afterQuery = base.TestQuery<Act>(o => o.Key == afterQuery.Key, 1).AsResultSet().First();
                Assert.AreEqual(3, afterQuery.LoadProperty(o => o.Participations).Count);
                Assert.AreEqual(0, afterQuery.LoadProperty(o => o.Protocols).Count);

                // Now update to remove the location participation
                base.TestUpdate(afterQuery, o =>
                {
                    o.LoadProperty(x => x.Participations).RemoveAll(r => r.ParticipationRoleKey == ActParticipationKeys.Location);
                    o.LoadProperty(x => x.Protocols).Add(
                        new ActProtocol()
                        {
                            Protocol = new Protocol()
                            {
                                Definition = new byte[] { 1, 2, 3, 4, 5, 6 },
                                HandlerClass = typeof(String),
                                Name = "Allergy Testing Protocol 2",
                                Oid = "2.25.948483938383"
                            },
                            Sequence = 1,
                            StateData = new byte[] { 2, 3, 4, 5, 6, 7 }
                        }
                    );
                    return o;
                });

                // No longer at GHHTF
                base.TestQuery<Act>(o => o.Participations.Where(g => g.ParticipationRoleKey == ActParticipationKeys.Location).Any(l => l.PlayerEntity.Names.Any(n => n.Component.Any(c => c.Value == "Good Health Hospital Testing Facility 2"))), 0);

                // Load 
                afterQuery = base.TestQuery<Act>(o => o.Key == afterQuery.Key, 1).AsResultSet().First();
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.Participations).Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Identifiers).Count); // no change to identifiers
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Extensions).Count); // no change to extensions
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Protocols).Count);

            }
        }

        /// <summary>
        /// Tests that an act is obsoleted 
        /// </summary>
        [Test]
        public void TestObsoleteAct()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var act = new Act()
                {
                    ClassConceptKey = ActClassKeys.Act,
                    MoodConceptKey = MoodConceptKeys.Goal,
                    TypeConceptKey = EntityClassKeys.Place,
                    ActTime = new DateTimeOffset(2022, 03, 03, 14, 23, 12, TimeSpan.Zero)
                };

                var afterInsert = base.TestInsert(act);
                Assert.IsNotNull(afterInsert.CreationTime);

                var fetch = base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1).AsResultSet();
                var afterFetch = fetch.First();
                Assert.AreEqual(StatusKeys.New, afterFetch.StatusConceptKey);

                var afterDelete = base.TestDelete(afterFetch, Core.Services.DeleteMode.LogicalDelete);
                base.TestQuery<Act>(o => o.Key == afterInsert.Key, 0);
                base.TestQuery<Act>(o => o.Key == afterInsert.Key && o.ObsoletionTime != null, 1);

                // Now un-delete
                var afterUndelete = base.TestUpdate(afterDelete, (o) =>
                {
                    o.StatusConceptKey = StatusKeys.Active;
                    return o;
                });
                Assert.AreEqual(StatusKeys.Active, afterUndelete.StatusConceptKey);

                // Re-query to ensure un-delete
                base.TestQuery<Act>(o => o.Key == afterInsert.Key, 1);

                // Now perma-delete
                afterDelete = base.TestDelete(afterDelete, Core.Services.DeleteMode.PermanentDelete);
                base.TestQuery<Act>(o => o.Key == afterInsert.Key, 0);
                base.TestQuery<Act>(o => o.Key == afterInsert.Key && StatusKeys.InactiveStates.Contains(o.StatusConceptKey.Value), 0);
            }
        }


        /// <summary>
        /// Tests a variety of queries in a multi-threaded environment
        /// </summary>
        [Test]
        public void TestActQuery()
        {
            using (AuthenticationContext.EnterSystemContext())
            {

                var aaUuid = this.GetOrCreateAA();

                Enumerable.Range(1, 99).AsParallel().ForAll(i =>
                {
                    using (AuthenticationContext.EnterSystemContext())
                    {
                        var act = new Act()
                        {
                            ClassConceptKey = ActClassKeys.Act,
                            MoodConceptKey = MoodConceptKeys.Intent,
                            TypeConceptKey = ObservationTypeKeys.Complaint,
                            ActTime = new DateTimeOffset(2020, 01, (i % 30 + 1), 0, 0, 0, new TimeSpan(-5, 0, 0)),
                            Identifiers = new List<ActIdentifier>()
                            {
                                new ActIdentifier(this.GetOrCreateAA(), $"TEST_ACT_{i}")
                            }
                        };

                        base.TestInsert(act);
                    }
                });

                var afterQuery = base.TestQuery<Act>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_ACT_%")), 99).AsResultSet() as IOrderableQueryResultSet<Act>;
                // Ordering should be newest first
                var sorted = afterQuery.OrderByDescending(o => o.VersionSequence);
                Assert.Greater(sorted.First().VersionSequence, sorted.Skip(1).First().VersionSequence);
                sorted = afterQuery.OrderBy(o => o.VersionSequence);
                // Ordering should be oldest first
                Assert.Less(sorted.First().VersionSequence, sorted.Skip(1).First().VersionSequence);

                // Order by date
                sorted = afterQuery.OrderByDescending(o => o.ActTime);
                Assert.Greater(sorted.First().ActTime, sorted.Skip(20).First().ActTime);
                sorted = afterQuery.OrderBy(o => o.ActTime);
                Assert.Less(sorted.First().ActTime, sorted.Skip(20).First().ActTime);

                // Stress the database query to ensure parallel queries
                Enumerable.Range(1, 9).AsParallel().ForAll(i =>
                {
                    var identifierTest = $"TEST_ACT_{i}%";
                    var afterLoadQuery = base.TestQuery<Act>(o => o.Identifiers.Any(d => d.Value.Contains(identifierTest)), 11).AsResultSet();

                    foreach (var l in afterLoadQuery)
                    {
                        Assert.AreEqual(1, l.LoadProperty(o => o.Identifiers).Count());
                    }
                });
            }
        }

        /// <summary>
        /// Test the deleteall function
        /// </summary>
        [Test]
        public void TestDeleteAll()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                for (var i = 0; i < 10; i++)
                {
                    var act = new Act()
                    {
                        ClassConceptKey = ActClassKeys.Act,
                        MoodConceptKey = ActMoodKeys.Goal,
                        TypeConceptKey = ObservationTypeKeys.CauseOfDeath,
                        ActTime = DateTimeOffset.Now,
                        Identifiers = new List<ActIdentifier>()
                        {
                            new ActIdentifier(this.GetOrCreateAA(), $"TEST_DELETE_{i}")
                        }
                    };

                    this.TestInsert(act);
                }

                var afterQuery = this.TestQuery<Act>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_DELETE_%")), 10);

                // Delete all
                var dpe = ApplicationServiceContext.Current.GetService<IDataPersistenceServiceEx<Act>>();
                using (DataPersistenceControlContext.Create(DeleteMode.LogicalDelete))
                {
                    dpe.DeleteAll(o => o.Identifiers.Any(i => i.Value.Contains("TEST_DELETE_%")), TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                }

                // Ensure no results on regular query
                afterQuery = this.TestQuery<Act>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_DELETE_%")), 0);
                // Ensure 10 results on inactive query
                afterQuery = this.TestQuery<Act>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_DELETE_%")) && o.ObsoletionTime.HasValue, 10);

                // Restore all
                var oldData = afterQuery.ToList().Select(q =>
                {
                    return this.TestUpdate(q, (r) =>
                    {
                        r.StatusConceptKey = StatusKeys.Active;
                        return r;
                    });
                }).ToList();

                // Ensure 10 now results
                afterQuery = this.TestQuery<Act>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_DELETE_%")), 10);

                // Now erase
                using (DataPersistenceControlContext.Create(DeleteMode.PermanentDelete))
                {
                    dpe.DeleteAll(o => o.Identifiers.Any(i => i.Value.Contains("TEST_DELETE_%")), TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                }
                // Ensure 0 now results
                afterQuery = this.TestQuery<Act>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_DELETE_%")), 0);
                // Ensure 0 results on inactive query
                afterQuery = this.TestQuery<Act>(o => o.Identifiers.Any(i => i.Value.Contains("TEST_DELETE_%")) && StatusKeys.InactiveStates.Contains(o.StatusConceptKey.Value), 0);
            }
        }
    }
}
