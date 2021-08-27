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

                var refTerm = new ReferenceTerm();

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
            }
        }

    }
}
