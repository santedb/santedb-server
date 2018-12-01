using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.ADO.Test.Core;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Test
{
    /// <summary>
    /// Concept persistence service test
    /// </summary>
    [TestClass]
    public class ConceptPersistenceServiceTest : PersistenceTest<Concept>
    {

        private static IPrincipal s_authorization;

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            s_authorization = AuthenticationContext.SystemPrincipal;
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);

            TestApplicationContext.TestAssembly = typeof(AdoIdentityProviderTest).Assembly;
            TestApplicationContext.Initialize(context.DeploymentDirectory);

        }

        /// <summary>
        /// Tests that the concept persistence service can successfully
        /// insert and retrieve a concept
        /// </summary>
        [TestMethod]
        public void TestInsertSimpleConcept()
        {
            Concept simpleConcept = new Concept()
            {
                ClassKey = ConceptClassKeys.Other,
                IsReadonly = true,
                Mnemonic = "TESTCODE1"
            };
            var afterTest = base.DoTestInsert(simpleConcept, s_authorization);
            Assert.AreEqual("TESTCODE1", afterTest.Mnemonic);
            Assert.AreEqual("Other", afterTest.Class.Mnemonic);
            Assert.IsTrue(afterTest.IsReadonly);
        }

        /// <summary>
        /// Tests that the concept persistence service can persist a 
        /// simple concept which has a display name
        /// </summary>
        [TestMethod]
        public void TestInsertNamedConcept()
        {
            Concept namedConcept = new Concept()
            {
                ClassKey = ConceptClassKeys.Other,
                IsReadonly = false,
                Mnemonic = "TESTCODE2"
            };
            
            // Names
            namedConcept.ConceptNames.Add(new ConceptName()
            {
                Name = "Test Code",
                Language = "en",
                PhoneticAlgorithm = PhoneticAlgorithm.EmptyAlgorithm
            });

            // Insert
            var afterTest = base.DoTestInsert(namedConcept, s_authorization);
            Assert.AreEqual("TESTCODE2", afterTest.Mnemonic);
            Assert.AreEqual("Other", afterTest.Class.Mnemonic);
            Assert.IsFalse(afterTest.IsReadonly);
            Assert.AreEqual(1, afterTest.LoadCollection<ConceptName>("ConceptNames").Count());
            Assert.AreEqual("en", afterTest.ConceptNames[0].Language);
            Assert.AreEqual("Test Code", afterTest.ConceptNames[0].Name);
        }

        /// <summary>
        /// Tests that the concept persistence service can persist a 
        /// simple concept which has a display name
        /// </summary>
        [TestMethod]
        public void TestUpdateNamedConcept()
        {

            Concept namedConcept = new Concept()
            {
                ClassKey = ConceptClassKeys.Other,
                IsReadonly = false,
                Mnemonic = "TESTCODE3"
            };

            // Names
            namedConcept.ConceptNames.Add(new ConceptName()
            {
                Name = "Test Code 1",
                Language = "en",
                PhoneticAlgorithm = PhoneticAlgorithm.EmptyAlgorithm,
                PhoneticCode = "E"
            });
            namedConcept.ConceptNames.Add(new ConceptName()
            {
                Name = "Test Code 2",
                Language = "en",
                PhoneticAlgorithm = PhoneticAlgorithm.EmptyAlgorithm,
                PhoneticCode = "E"
            });

            // Insert
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Concept>>();
            var afterTest = persistenceService.Insert(namedConcept, TransactionMode.Commit, s_authorization);

            Assert.AreEqual("TESTCODE3", afterTest.Mnemonic);
            Assert.AreEqual("Other", afterTest.LoadProperty<ConceptClass>("Class").Mnemonic);
            Assert.IsFalse(afterTest.IsReadonly);
            Assert.AreEqual(2, afterTest.LoadCollection<ConceptName>("ConceptNames").Count());
            Assert.AreEqual("en", afterTest.ConceptNames[0].Language);
            Assert.IsTrue(afterTest.LoadCollection<ConceptName>("ConceptNames").Any(n => n.Name == "Test Code 1"));
            Assert.AreEqual("E", afterTest.ConceptNames[0].PhoneticCode);
            Assert.IsNotNull(afterTest.LoadProperty<SecurityUser>("CreatedBy"));

            var originalId = afterTest.VersionKey;

            // Step 1: Test an ADD of a name
            afterTest.ConceptNames.Add(new ConceptName()
            {
                Name = "Test Code 3",
                Language = "en",
                PhoneticAlgorithm = PhoneticAlgorithm.EmptyAlgorithm,
                PhoneticCode = "E"
            });
            afterTest.Mnemonic = "TESTCODE3_A";
            afterTest = persistenceService.Update(afterTest, TransactionMode.Commit, s_authorization);
            Assert.AreEqual(3, afterTest.LoadCollection<ConceptName>("ConceptNames").Count());
            Assert.AreEqual("TESTCODE3_A", afterTest.Mnemonic);
            Assert.IsNotNull(afterTest.GetPreviousVersion());
            Assert.AreEqual(originalId, afterTest.PreviousVersionKey);
            var updateKey = afterTest.VersionKey;

            // Verify 2: Remove a name
            afterTest.ConceptNames.RemoveAt(1);
            afterTest.ConceptNames[0].Language = "fr";
            afterTest = persistenceService.Update(afterTest, TransactionMode.Commit, s_authorization);
            Assert.AreEqual(2, afterTest.LoadCollection<ConceptName>("ConceptNames").Count());
            Assert.IsTrue(afterTest.LoadCollection<ConceptName>("ConceptNames").Any(n => n.Language == "fr"));
            Assert.IsNotNull(afterTest.GetPreviousVersion());
            Assert.AreEqual(updateKey, afterTest.PreviousVersionKey);
            Assert.IsNotNull(afterTest.GetPreviousVersion().GetPreviousVersion());
            Assert.AreEqual(originalId, afterTest.GetPreviousVersion().PreviousVersionKey);
        }

        /// <summary>
        /// Tests that the concept persistence service can persist a 
        /// simple concept which has a display name
        /// </summary>
        [TestMethod]
        public void TestInsertReferenceTermConcept()
        {
            Concept refTermConcept = new Concept()
            {
                ClassKey = ConceptClassKeys.Other,
                IsReadonly = false,
                Mnemonic = "TESTCODE5"
            };

            // Names
            refTermConcept.ConceptNames.Add(new ConceptName()
            {
                Name = "Test Code",
                Language = "en"
            });

            // Reference term
            refTermConcept.ReferenceTerms.Add(new ConceptReferenceTerm()
            {
                RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                ReferenceTerm = new ReferenceTerm()
                {
                    CodeSystemKey = CodeSystemKeys.LOINC,
                    Mnemonic = "X-4039503-403"
                }
            });

            // Insert
            var afterTest = base.DoTestInsert(refTermConcept, s_authorization);
            Assert.AreEqual("TESTCODE5", afterTest.Mnemonic);
            Assert.AreEqual("Other", afterTest.Class.Mnemonic);
            Assert.IsFalse(afterTest.IsReadonly);
            Assert.AreEqual(1, afterTest.LoadCollection<ConceptName>("ConceptNames").Count());
            Assert.AreEqual(1, afterTest.ReferenceTerms.Count);
            Assert.AreEqual("en", afterTest.ConceptNames[0].Language);
            Assert.AreEqual(ConceptRelationshipTypeKeys.SameAs, afterTest.ReferenceTerms[0].RelationshipTypeKey);
            Assert.IsNotNull(afterTest.ReferenceTerms[0].LoadProperty<ConceptRelationshipType>("RelationshipType"));
            Assert.IsNotNull(afterTest.ReferenceTerms[0].LoadProperty<ReferenceTerm>("ReferenceTerm"));
            Assert.AreEqual(CodeSystemKeys.LOINC, afterTest.ReferenceTerms[0].ReferenceTerm.LoadProperty<CodeSystem>("CodeSystem").Key);
            Assert.AreEqual("Test Code", afterTest.ConceptNames[0].Name);
        }


        /// <summary>
        /// Tests that the concept persistence service can persist a 
        /// simple concept which has a display name
        /// </summary>
        [TestMethod]
        public void TestUpdateConceptReferenceTerm()
        {
            Concept refTermConcept = new Concept()
            {
                ClassKey = ConceptClassKeys.Other,
                IsReadonly = false,
                Mnemonic = "TESTCODE6"
            };

            // Names
            refTermConcept.ConceptNames.Add(new ConceptName()
            {
                Name = "Test Code",
                Language = "en",
                PhoneticAlgorithm = PhoneticAlgorithm.EmptyAlgorithm,
                PhoneticCode = "E"
            });

            // Reference term
            refTermConcept.ReferenceTerms.Add(new ConceptReferenceTerm()
            {
                RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                ReferenceTerm = new ReferenceTerm()
                {
                    CodeSystemKey = CodeSystemKeys.LOINC,
                    Mnemonic = "X-4039503-402"
                }
            });

            // Insert
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Concept>>();
            var afterTest = persistenceService.Insert(refTermConcept, TransactionMode.Commit, s_authorization);

            Assert.AreEqual("TESTCODE6", afterTest.Mnemonic);
            Assert.AreEqual("Other", afterTest.LoadProperty<ConceptClass>("Class").Mnemonic);
            Assert.IsFalse(afterTest.IsReadonly);
            Assert.AreEqual(1, afterTest.LoadCollection<ConceptName>("ConceptNames").Count());
            Assert.AreEqual(1, afterTest.LoadCollection<ConceptReferenceTerm>("ReferenceTerms").Count());
            Assert.AreEqual("en", afterTest.ConceptNames[0].Language);
            Assert.AreEqual(ConceptRelationshipTypeKeys.SameAs, afterTest.ReferenceTerms[0].RelationshipTypeKey);
            Assert.IsNotNull(afterTest.ReferenceTerms[0].LoadProperty<ConceptRelationshipType>("RelationshipType"));
            Assert.IsNotNull(afterTest.ReferenceTerms[0].LoadProperty<ReferenceTerm>("ReferenceTerm"));
            Assert.AreEqual(CodeSystemKeys.LOINC, afterTest.ReferenceTerms[0].ReferenceTerm.LoadProperty<CodeSystem>("CodeSystem").Key);
            Assert.AreEqual("Test Code", afterTest.ConceptNames[0].Name);
            Assert.AreEqual("E", afterTest.ConceptNames[0].PhoneticCode);

            // Update
            afterTest.ReferenceTerms.Add(new ConceptReferenceTerm()
            {
                RelationshipTypeKey = ConceptRelationshipTypeKeys.SameAs,
                ReferenceTerm = new ReferenceTerm()
                {
                    CodeSystemKey = CodeSystemKeys.LOINC,
                    Mnemonic = "X-4039503-408"
                }
            });
            afterTest = persistenceService.Update(afterTest, TransactionMode.Commit, s_authorization);
            Assert.AreEqual(2, afterTest.LoadCollection<ConceptReferenceTerm>("ReferenceTerms").Count());
            Assert.IsTrue(afterTest.ReferenceTerms.Any(o => o.ReferenceTerm.Mnemonic == "X-4039503-408"));

            // Remove one
            afterTest.ReferenceTerms.RemoveAt(0);
            afterTest = persistenceService.Update(afterTest, TransactionMode.Commit, s_authorization);
            Assert.AreEqual(1, afterTest.LoadCollection<ConceptReferenceTerm>("ReferenceTerms").Count());

        }
    }
}
