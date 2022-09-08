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
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core;
using System.Diagnostics.CodeAnalysis;
using SanteDB.Core.Services;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Concept persistence service test
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class ConceptPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Tests that the persistence layer can store data related to a simple concept with no extended props
        /// </summary>
        [Test]
        public void TestInsertConcept()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var concept = new Concept()
                {
                    Mnemonic = "TEST-01",
                    ClassKey = ConceptClassKeys.Other,
                    StatusConceptKey = StatusKeys.Active
                };

                var afterInsert = base.TestInsert(concept);
                Assert.AreEqual("TEST-01", afterInsert.Mnemonic);
                Assert.AreEqual(ConceptClassKeys.Other, afterInsert.ClassKey);

                // Fetch
                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-01", 1).FirstOrDefault();
                Assert.AreEqual(ConceptClassKeys.Other, afterQuery.ClassKey);
                Assert.AreEqual("TEST-01", afterQuery.Mnemonic);
            }
        }

        /// <summary>
        /// Test insert of extended insert of concept
        /// </summary>
        [Test]
        public void TestInsertConceptWithNames()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var concept = new Concept()
                {
                    Mnemonic = "TEST-02",
                    ClassKey = ConceptClassKeys.Other,
                    StatusConceptKey = StatusKeys.Active,
                    ConceptNames = new List<ConceptName>()
                    {
                        new ConceptName("This is a test concept")
                    }
                };

                var afterInsert = base.TestInsert(concept);
                Assert.AreEqual("TEST-02", afterInsert.Mnemonic);
                Assert.AreEqual(ConceptClassKeys.Other, afterInsert.ClassKey);
                Assert.AreEqual(1, afterInsert.ConceptNames.Count);

                // Fetch
                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-02", 1).FirstOrDefault();
                Assert.AreEqual(ConceptClassKeys.Other, afterQuery.ClassKey);
                Assert.AreEqual("TEST-02", afterQuery.Mnemonic);

                // Rule 1: The names are empty
                Assert.AreEqual(1, afterQuery.LoadCollection(o => o.ConceptNames).Count());
                Assert.AreEqual("This is a test concept", afterQuery.LoadCollection(o => o.ConceptNames).First().Name);
            }
        }

        /// <summary>
        /// Test insert of extended insert of concept
        /// </summary>
        [Test]
        public void TestInsertConceptWithTerms()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var concept = new Concept()
                {
                    Mnemonic = "TEST-03",
                    ClassKey = ConceptClassKeys.Other,
                    StatusConceptKey = StatusKeys.Active,
                    ConceptNames = new List<ConceptName>()
                    {
                        new ConceptName("This is a test concept 2")
                    },
                    ReferenceTerms = new List<ConceptReferenceTerm>()
                    {
                        new ConceptReferenceTerm()
                        {
                            RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                            ReferenceTerm = new ReferenceTerm()
                            {
                                CodeSystemKey = CodeSystemKeys.CVX,
                                Mnemonic = "032XX",
                                DisplayNames = new List<ReferenceTermName>()
                                {
                                    new ReferenceTermName("This is a referene term")
                                }
                            }
                        }
                    }
                };

                var afterInsert = base.TestInsert(concept);
                Assert.AreEqual("TEST-03", afterInsert.Mnemonic);
                Assert.AreEqual(ConceptClassKeys.Other, afterInsert.ClassKey);
                Assert.AreEqual(1, afterInsert.ConceptNames.Count);
                Assert.AreEqual(1, afterInsert.ReferenceTerms.Count);
                Assert.AreEqual("032XX", afterInsert.ReferenceTerms.First().LoadProperty(o => o.ReferenceTerm).Mnemonic);
                Assert.AreEqual(ConceptRelationshipTypeKeys.SameAs, afterInsert.ReferenceTerms.First().RelationshipTypeKey);

                // Fetch
                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-03", 1).FirstOrDefault();
                Assert.AreEqual(ConceptClassKeys.Other, afterQuery.ClassKey);
                Assert.AreEqual("TEST-03", afterQuery.Mnemonic);

                // Rule 1: The names are empty
                Assert.AreEqual(1, afterQuery.LoadCollection(o => o.ConceptNames).Count());

                // Rule 2: Reference terms are empty until loaded
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);
                Assert.AreEqual(ConceptRelationshipTypeKeys.SameAs, afterQuery.ReferenceTerms.First().RelationshipTypeKey);
                Assert.AreEqual("032XX", afterQuery.ReferenceTerms.First().LoadProperty(o => o.ReferenceTerm).Mnemonic);
                Assert.AreEqual(1, afterQuery.ReferenceTerms.First().ReferenceTerm.LoadProperty(o => o.DisplayNames).Count);
            }
        }

        /// <summary>
        /// Tests that a concept with full properties specified is inserted properly
        /// </summary>
        [Test]
        public void TestInsertFullConcept()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var concept = new Concept()
                {
                    Mnemonic = "TEST-04",
                    ClassKey = ConceptClassKeys.Other,
                    ConceptNames = new List<ConceptName>()
                    {
                        new ConceptName("This is yet another test concept")
                    },
                    ConceptSetsXml = new List<Guid>()
                    {
                        ConceptSetKeys.AdministrativeGenderCode
                    },
                    ReferenceTerms = new List<ConceptReferenceTerm>()
                    {
                        new ConceptReferenceTerm()
                        {
                            RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                            ReferenceTerm = new ReferenceTerm()
                            {
                                CodeSystemKey = CodeSystemKeys.AdministrativeGender,
                                Mnemonic = "Foo",
                                DisplayNames = new List<ReferenceTermName>()
                                {
                                    new ReferenceTermName("Foo is not female nor male, but foo")
                                }
                            }
                        }
                    },
                    StatusConceptKey = StatusKeys.Active,
                    Relationships = new List<ConceptRelationship>()
                    {
                        new ConceptRelationship(ConceptRelationshipTypeKeys.SameAs, NullReasonKeys.NotApplicable)
                    }
                };

                var afterInsert = base.TestInsert(concept);
                Assert.AreEqual("TEST-04", afterInsert.Mnemonic);
                Assert.AreEqual(StatusKeys.Active, afterInsert.StatusConceptKey);
                Assert.AreEqual(1, afterInsert.Relationships.Count);
                Assert.AreEqual(1, afterInsert.ConceptNames.Count);
                Assert.AreEqual(1, afterInsert.ReferenceTerms.Count);
                Assert.AreEqual(1, afterInsert.ConceptSetsXml.Count);

                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-04", 1).First();
                Assert.AreEqual("TEST-04", afterQuery.Mnemonic);
                Assert.AreEqual(StatusKeys.Active, afterQuery.StatusConceptKey);

                // Relationships should not have data accoring to load condition
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Relationships).Count);
                Assert.AreEqual(ConceptRelationshipTypeKeys.SameAs, afterQuery.Relationships.First().RelationshipTypeKey);
                Assert.AreEqual(NullReasonKeys.NotApplicable, afterQuery.Relationships.First().TargetConceptKey);
                Assert.IsNotNull(afterQuery.Relationships.First().LoadProperty(o => o.TargetConcept));

                // Ensure that the concept names count is 0
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ConceptNames).Count);

                // Ensure reference terms are 0 until loaded
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);

                // Concept sets are not a delay loadable property
                Assert.AreEqual(1, afterQuery.ConceptSetsXml.Count);
            }
        }

        /// <summary>
        /// Tests that basic versioned concept updates work properly
        /// </summary>
        [Test]
        public void TestUpdateBasicConcept()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var concept = new Concept()
                {
                    Mnemonic = "TEST-05",
                    ClassKey = ConceptClassKeys.Other,
                    StatusConceptKey = StatusKeys.Active
                };

                var afterInsert = base.TestInsert(concept);
                Assert.AreEqual("TEST-05", afterInsert.Mnemonic);
                Assert.AreEqual(ConceptClassKeys.Other, afterInsert.ClassKey);
                Assert.AreEqual(StatusKeys.Active, afterInsert.StatusConceptKey);

                // Before update the current version is TEST-05
                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-05", 1);

                // Perform update
                var afterUpdate = base.TestUpdate(afterInsert, (o) => { o.Mnemonic = "TEST-05B"; return o; });
                Assert.AreEqual("TEST-05B", afterUpdate.Mnemonic);
                Assert.AreEqual(ConceptClassKeys.Other, afterUpdate.ClassKey);
                Assert.AreEqual(StatusKeys.Active, afterUpdate.StatusConceptKey);

                // Test we can retrieve the previous version
                Assert.IsNotNull(afterUpdate.GetPreviousVersion());
                Assert.AreEqual(afterInsert.VersionKey, afterUpdate.PreviousVersionKey);
                Assert.AreEqual("TEST-05", afterUpdate.GetPreviousVersion().Mnemonic);

                // Query for TEST-05 should return 0 results
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-05", 0);
                // Query for TEST-05B returns 1 result
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-05B", 1);
                var queryConcept = afterQuery.First();

                // Ensure load is proper
                Assert.AreEqual(afterInsert.VersionKey, queryConcept.PreviousVersionKey);
                Assert.IsNotNull(queryConcept.GetPreviousVersion());
                Assert.AreEqual("TEST-05B", queryConcept.Mnemonic);
                Assert.AreEqual("TEST-05", queryConcept.GetPreviousVersion().Mnemonic);
            }
        }

        /// <summary>
        /// This test ensures that the persistence layer is able to remove and add new related objects to the concept
        /// </summary>
        [Test]
        public void TestUpdateRelatedConceptObjects()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var concept = new Concept()
                {
                    Mnemonic = "TEST-06",
                    ClassKey = ConceptClassKeys.Other,
                    StatusConceptKey = StatusKeys.Active,
                    ConceptNames = new List<ConceptName>()
                    {
                        new ConceptName("A new concept"),
                        new ConceptName("fr", "Un concept nouveau"),
                        new ConceptName("es", "Un concepto nuevo")
                    },
                    ConceptSetsXml = new List<Guid>()
                    {
                        ConceptSetKeys.AdministrativeGenderCode
                    },
                    ReferenceTerms = new List<ConceptReferenceTerm>()
                    {
                        new ConceptReferenceTerm()
                        {
                            RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                            ReferenceTerm = new ReferenceTerm("NEW", CodeSystemKeys.ICD9)
                        }
                    }
                };

                var afterInsert = base.TestInsert(concept);
                Assert.AreEqual("TEST-06", afterInsert.Mnemonic);
                Assert.AreEqual(3, afterInsert.ConceptNames.Count);
                Assert.AreEqual(1, afterInsert.ConceptSetsXml.Count);
                Assert.AreEqual(1, afterInsert.ReferenceTerms.Count);

                // First we will remove a name
                var afterUpdate = base.TestUpdate(afterInsert, (o) =>
                {
                    o.ConceptNames.RemoveAt(2);
                    return o;
                });
                Assert.AreEqual(2, afterUpdate.ConceptNames.Count);

                // Query to ensure the name is not being loaded
                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-06", 1).FirstOrDefault();
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.ConceptNames).Count);
                Assert.AreEqual("en", afterQuery.ConceptNames[0].Language);
                Assert.AreEqual("fr", afterQuery.ConceptNames[1].Language);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);

                // Next, we'll add something that never existed before
                afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.LoadProperty(r => r.Relationships).Add(new ConceptRelationship(ConceptRelationshipTypeKeys.SameAs, NullReasonKeys.NoInformation));
                    return o;
                });
                Assert.AreEqual(1, afterUpdate.Relationships.Count);
                Assert.AreEqual(2, afterUpdate.ConceptNames.Count);

                // Ensure the query returns the same
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-06", 1).FirstOrDefault();
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.ConceptNames).Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Relationships).Count);
                Assert.AreEqual("en", afterQuery.ConceptNames[0].Language);
                Assert.AreEqual("fr", afterQuery.ConceptNames[1].Language);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);

                // Next, we'll update a few properties
                afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.ConceptNames.RemoveAt(1);
                    o.ConceptSetsXml.Clear();
                    o.ReferenceTerms.Add(new ConceptReferenceTerm()
                    {
                        RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                        ReferenceTerm = new ReferenceTerm("OTH203", CodeSystemKeys.ICD10CM)
                    });
                    return o;
                });

                // Ensure the update reflects
                Assert.AreEqual(1, afterUpdate.ConceptNames.Count);
                Assert.AreEqual(2, afterUpdate.ReferenceTerms.Count);
                Assert.AreEqual(0, afterUpdate.ConceptSetsXml.Count);
                Assert.AreEqual(1, afterUpdate.Relationships.Count);

                // Now re-query
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-06", 1).FirstOrDefault();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ConceptNames).Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Relationships).Count);
                Assert.AreEqual("en", afterQuery.ConceptNames[0].Language);
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);
                Assert.AreEqual(0, afterQuery.ConceptSetsXml.Count);
            }
        }

        /// <summary>
        /// Tests the various query functions in the concept persistence service.
        /// </summary>
        [Test]
        public void TestQueryConceptWithOrdering()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var concept1 = new Concept()
                {
                    Mnemonic = "TEST-07A",
                    ClassKey = ConceptClassKeys.Other,
                    StatusConceptKey = StatusKeys.Active,
                    ReferenceTerms = new List<ConceptReferenceTerm>()
                    {
                        new ConceptReferenceTerm()
                        {
                            RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                            ReferenceTerm = new ReferenceTerm()
                            {
                                Mnemonic = "TEST07A",
                                CodeSystemKey = CodeSystemKeys.AdministrativeGender
                            }
                        }
                    },
                    ConceptNames = new List<ConceptName>()
                    {
                        new ConceptName("en", "Seven A"),
                        new ConceptName("fr", "Sept A")
                    }
                };
                var concept2 = new Concept()
                {
                    Mnemonic = "TEST-07B",
                    ClassKey = ConceptClassKeys.Other,
                    StatusConceptKey = StatusKeys.Active,
                    ReferenceTerms = new List<ConceptReferenceTerm>()
                    {
                        new ConceptReferenceTerm()
                        {
                            RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                            ReferenceTerm = new ReferenceTerm()
                            {
                                Mnemonic = "TEST07B",
                                CodeSystemKey = CodeSystemKeys.AdministrativeGender
                            }
                        }
                    },
                    ConceptNames = new List<ConceptName>()
                    {
                        new ConceptName("en", "Seven B"),
                        new ConceptName("fr", "Sept B")
                    }
                };

                var after1 = base.TestInsert(concept1);
                var after2 = base.TestInsert(concept2);

                Assert.IsTrue(after1.VersionSequence < after2.VersionSequence);

                // Query filter on mnemonic (simple)
                base.TestQuery<Concept>(o => o.Mnemonic == "TEST-07A" || o.Mnemonic == "TEST-07B", 2);
                base.TestQuery<Concept>(o => o.Mnemonic == "TEST-07A", 1);

                // Query filter on name
                base.TestQuery<Concept>(o => o.ConceptNames.Any(n => n.Language == "en" && n.Name.StartsWith("Seven")), 2);
                base.TestQuery<Concept>(o => o.ConceptNames.Any(n => n.Language == "fr" && n.Name.StartsWith("Seven")), 0);

                // Test filte ron reference term
                base.TestQuery<Concept>(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.Mnemonic == "TEST07A"), 1);

                // Test ordering
                var result = base.TestQuery<Concept>(o => o.Mnemonic.StartsWith("TEST-07"), 2);
                var queryId = Guid.NewGuid();

                // Ordering
                Assert.AreEqual("TEST-07A", result.OrderBy(o => o.VersionSequence).First().Mnemonic);
                Assert.AreEqual("TEST-07B", result.OrderByDescending(o => o.VersionSequence).First().Mnemonic);

                // Stateful queries
                ApplicationServiceContext.Current.GetService<TestQueryPersistenceService>().SetExpectedQueryStats(queryId, 2);
                var stateful = result.OrderBy(o => o.VersionSequence).AsResultSet().AsStateful(queryId);

                Assert.AreEqual(2, stateful.Count());

                // Test that nested selectors work
                var uuids = result.Select(o => o.Key);
                Assert.AreEqual(after1.Key, uuids.First());
                Assert.AreEqual(after2.Key, uuids.Last());
            }
        }

        /// <summary>
        /// Tests that a concept can be obsoleted
        /// </summary>
        [Test]
        public void TestObsoleteConcept()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var concept = new Concept()
                {
                    Mnemonic = "TEST-08",
                    ClassKey = ConceptClassKeys.Other,
                    ConceptNames = new List<ConceptName>()
                    {
                        new ConceptName("en", "I will be removed")
                    },
                    ReferenceTerms = new List<ConceptReferenceTerm>()
                    {
                        new ConceptReferenceTerm()
                        {
                            RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                            ReferenceTerm = new ReferenceTerm("TEST08", CodeSystemKeys.AdministrativeGender)
                        }
                    }
                };

                var afterInsert = base.TestInsert(concept);
                Assert.AreEqual("TEST-08", afterInsert.Mnemonic);
                Assert.AreEqual(StatusKeys.New, afterInsert.StatusConceptKey);

                // Now query
                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-08", 1).FirstOrDefault();
                afterQuery = base.TestQuery<Concept>(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.Mnemonic == "TEST08"), 1).FirstOrDefault();

                // Now obsolete
                var afterObsolete = base.TestDelete(afterInsert, Core.Services.DeleteMode.LogicalDelete);
                Assert.AreEqual(afterInsert.StatusConceptKey, afterObsolete.StatusConceptKey); // status does not change
                
                // Should not be returned in query results
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-08", 0).FirstOrDefault();
                Assert.IsNull(afterQuery);
                afterQuery = base.TestQuery<Concept>(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.Mnemonic == "TEST08"), 0).FirstOrDefault();
                Assert.IsNull(afterQuery);

                // Retrieve should result in an object
                var afterFetch = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Concept>>().Get(afterInsert.Key.Value, null, AuthenticationContext.SystemPrincipal);
                Assert.IsNotNull(afterFetch);

                // Should return if Obsolete time is specifically set
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-08" && o.ObsoletionTime != null, 1).FirstOrDefault();
                Assert.AreEqual("TEST-08", afterQuery.Mnemonic);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ConceptNames).Count);

                // Should un-delete
                var afterRestore = base.TestUpdate(afterInsert, (o) =>
                {
                    o.StatusConceptKey = StatusKeys.Active;
                    return o;
                });

                // We should be able to fetch previous versions
                var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Concept>>();
                var oldVersion = persistenceService.Get(afterRestore.Key.Value, afterRestore.VersionKey, AuthenticationContext.SystemPrincipal);
                Assert.IsNotNull(oldVersion);
                oldVersion = persistenceService.Get(afterRestore.Key.Value, oldVersion.PreviousVersionKey, AuthenticationContext.SystemPrincipal);
                Assert.IsNotNull(oldVersion);
                Assert.AreNotEqual(oldVersion.VersionKey, afterRestore.VersionKey);
                Assert.AreEqual(oldVersion.VersionKey, afterRestore.PreviousVersionKey);
                // Should now be returned in query results since it is restored
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-08", 1).FirstOrDefault();
                Assert.IsNotNull(afterQuery);
                afterQuery = base.TestQuery<Concept>(o => o.ReferenceTerms.Any(r => r.ReferenceTerm.Mnemonic == "TEST08"), 1).FirstOrDefault();
                Assert.IsNotNull(afterQuery);

                // Test Nullify
                var afterNullify = base.TestDelete(afterRestore, Core.Services.DeleteMode.LogicalDelete);
                // Should not be returned in query results since it is restored
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-08", 0).FirstOrDefault();
                Assert.IsNull(afterQuery);
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-08" && o.ObsoletionTime.HasValue, 1).FirstOrDefault();
                Assert.IsNotNull(afterQuery);

                // Now test purge
                var afterPurge = base.TestDelete(afterRestore, Core.Services.DeleteMode.PermanentDelete);
                // Should not be returned in query results since it is restored
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-08", 0).FirstOrDefault();
                Assert.IsNull(afterQuery);

                try
                {
                    // Should not be able to restore
                    afterPurge = base.TestUpdate(afterInsert, (o) =>
                    {
                        o.StatusConceptKey = StatusKeys.Active;
                        return o;
                    });
                    Assert.Fail("Should have failed");
                }
                catch
                {
                }
            }
        }
    }
}