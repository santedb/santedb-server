/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;

namespace SanteDB.Persistence.Data.ADO.Test
{
    /// <summary>
    /// Concept persistence service test
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestFixture(Category = "Persistence")]
    public class ConceptPersistenceServiceTest : PersistenceTest<Concept>
    {

        private static IPrincipal s_authorization;

        [SetUp]
        public void ClassSetup()
        {
            s_authorization = AuthenticationContext.SystemPrincipal;
            AuthenticationContext.EnterSystemContext();


        }

        /// <summary>
        /// Tests that the concept persistence service can successfully
        /// insert and retrieve a concept
        /// </summary>
        [Test]
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
        [Test]
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
        [Test]
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
                Language = "en"
            });
            namedConcept.ConceptNames.Add(new ConceptName()
            {
                Name = "Test Code 2",
                Language = "en"
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
            Assert.IsNotNull(afterTest.LoadProperty<SecurityProvenance>("CreatedBy"));

            var originalId = afterTest.VersionKey;

            // Step 1: Test an ADD of a name
            afterTest.ConceptNames.Add(new ConceptName()
            {
                Name = "Test Code 3",
                Language = "en"
            });
            afterTest.Mnemonic = "TESTCODE3_A";
            afterTest = persistenceService.Update(afterTest, TransactionMode.Commit, s_authorization);
            Assert.AreEqual(3, afterTest.LoadCollection<ConceptName>("ConceptNames").Count());
            Assert.AreEqual(originalId, afterTest.PreviousVersionKey);
            Assert.AreEqual("TESTCODE3_A", afterTest.Mnemonic);
            Assert.IsNotNull(afterTest.GetPreviousVersion());
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
        [Test]
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
        [Test]
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
                Language = "en"
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
