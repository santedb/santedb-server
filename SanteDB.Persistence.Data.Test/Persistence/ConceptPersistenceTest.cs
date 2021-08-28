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

    }
}
