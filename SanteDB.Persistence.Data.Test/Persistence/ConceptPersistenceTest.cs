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

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Concept persistence service test
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO Concepts")]
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
                Assert.AreEqual(0, afterQuery.ConceptNames.Count);
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
                Assert.AreEqual("032XX", afterInsert.ReferenceTerms.First().ReferenceTerm.Mnemonic);
                Assert.AreEqual(ConceptRelationshipTypeKeys.SameAs, afterInsert.ReferenceTerms.First().RelationshipTypeKey);

                // Fetch
                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-03", 1).FirstOrDefault();
                Assert.AreEqual(ConceptClassKeys.Other, afterQuery.ClassKey);
                Assert.AreEqual("TEST-03", afterQuery.Mnemonic);

                // Rule 1: The names are empty
                Assert.AreEqual(0, afterQuery.ConceptNames.Count);
                Assert.AreEqual(1, afterQuery.LoadCollection(o => o.ConceptNames).Count());

                // Rule 2: Reference terms are empty until loaded
                Assert.AreEqual(0, afterQuery.ReferenceTerms.Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);
                Assert.AreEqual(ConceptRelationshipTypeKeys.SameAs, afterQuery.ReferenceTerms.First().RelationshipTypeKey);
                Assert.IsNull(afterQuery.ReferenceTerms.First().ReferenceTerm);
                Assert.AreEqual("032XX", afterQuery.ReferenceTerms.First().LoadProperty(o => o.ReferenceTerm).Mnemonic);
                Assert.AreEqual(0, afterQuery.ReferenceTerms.First().ReferenceTerm.DisplayNames.Count);
                Assert.AreEqual(1, afterQuery.ReferenceTerms.First().ReferenceTerm.LoadProperty(o => o.DisplayNames).Count);
            }
        }

        /// <summary>
        /// Tests that a concept with full properties specified is inserted properly
        /// </summary>
        [Test]
        public void TestInsertFullConcept()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var concept = new Concept()
                {
                    Mnemonic = "TEST-04",
                    ClassKey = ConceptClassKeys.Other,
                    ConceptNames = new List<ConceptName>()
                    {
                        new ConceptName("This is yet another test concept")
                    },
                    ConceptSetKeys = new List<Guid>()
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
                    Relationship = new List<ConceptRelationship>()
                    {
                        new ConceptRelationship(ConceptRelationshipTypeKeys.SameAs, NullReasonKeys.NotApplicable)
                    }
                };

                var afterInsert = base.TestInsert(concept);
                Assert.AreEqual("TEST-04", afterInsert.Mnemonic);
                Assert.AreEqual(StatusKeys.Active, afterInsert.StatusConceptKey);
                Assert.AreEqual(1, afterInsert.Relationship.Count);
                Assert.AreEqual(1, afterInsert.ConceptNames.Count);
                Assert.AreEqual(1, afterInsert.ReferenceTerms.Count);
                Assert.AreEqual(1, afterInsert.ConceptSetKeys.Count);

                var afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-04", 1).First();
                Assert.AreEqual("TEST-04", afterQuery.Mnemonic);
                Assert.AreEqual(StatusKeys.Active, afterQuery.StatusConceptKey);
                
                // Relationships should not have data accoring to load condition
                Assert.AreEqual(0, afterQuery.Relationship.Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Relationship).Count);
                Assert.AreEqual(ConceptRelationshipTypeKeys.SameAs, afterQuery.Relationship.First().RelationshipTypeKey);
                Assert.AreEqual(NullReasonKeys.NotApplicable, afterQuery.Relationship.First().TargetConceptKey);
                Assert.IsNull(afterQuery.Relationship.First().TargetConcept);
                Assert.IsNotNull(afterQuery.Relationship.First().LoadProperty(o => o.TargetConcept));

                // Ensure that the concept names count is 0
                Assert.AreEqual(0, afterQuery.ConceptNames.Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ConceptNames).Count);

                // Ensure reference terms are 0 until loaded
                Assert.AreEqual(0, afterQuery.ReferenceTerms.Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);

                // Concept sets are not a delay loadable property 
                Assert.AreEqual(1, afterQuery.ConceptSetKeys.Count);
            }
        }

        /// <summary>
        /// Tests that basic versioned concept updates work properly
        /// </summary>
        [Test]
        public void TestUpdateBasicConcept()
        {
            using(AuthenticationContext.EnterSystemContext())
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
            using(AuthenticationContext.EnterSystemContext())
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
                    ConceptSetKeys = new List<Guid>()
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
                Assert.AreEqual(1, afterInsert.ConceptSetKeys.Count);
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
                Assert.AreEqual(0, afterQuery.ConceptNames.Count);
                Assert.AreEqual(2, afterQuery.LoadProperty(o=>o.ConceptNames).Count);
                Assert.AreEqual("en", afterQuery.ConceptNames[0].Language);
                Assert.AreEqual("fr", afterQuery.ConceptNames[1].Language);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);

                // Next, we'll add something that never existed before 
                afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.Relationship.Add(new ConceptRelationship(ConceptRelationshipTypeKeys.SameAs, NullReasonKeys.NoInformation));
                    return o;
                });
                Assert.AreEqual(1, afterUpdate.Relationship.Count);
                Assert.AreEqual(2, afterUpdate.ConceptNames.Count);

                // Ensure the query returns the same
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-06", 1).FirstOrDefault();
                Assert.AreEqual(0, afterQuery.ConceptNames.Count);
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.ConceptNames).Count);
                Assert.AreEqual(0, afterQuery.Relationship.Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Relationship).Count);
                Assert.AreEqual("en", afterQuery.ConceptNames[0].Language);
                Assert.AreEqual("fr", afterQuery.ConceptNames[1].Language);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);


                // Next, we'll update a few properties
                afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.ConceptNames.RemoveAt(1);
                    o.ConceptSetKeys.Clear();
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
                Assert.AreEqual(0, afterUpdate.ConceptSetKeys.Count);
                Assert.AreEqual(1, afterUpdate.Relationship.Count);

                // Now re-query
                afterQuery = base.TestQuery<Concept>(o => o.Mnemonic == "TEST-06", 1).FirstOrDefault();
                Assert.AreEqual(0, afterQuery.ConceptNames.Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.ConceptNames).Count);
                Assert.AreEqual(0, afterQuery.Relationship.Count);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Relationship).Count);
                Assert.AreEqual("en", afterQuery.ConceptNames[0].Language);
                Assert.AreEqual(2, afterQuery.LoadProperty(o => o.ReferenceTerms).Count);
                Assert.AreEqual(0, afterQuery.ConceptSetKeys.Count);

            }
        }
    }
}
