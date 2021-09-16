﻿using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Test fixture
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO Concept Set")]
    public class ConceptSetPersistenceServiceTest : DataPersistenceTest
    {

        /// <summary>
        /// Test query concept
        /// </summary>
        [Test]
        public void TestQueryConcept()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var conceptSet = base.TestQuery<ConceptSet>(o => o.Mnemonic == "NullReason", 1).AsResultSet();
                Assert.AreEqual(15, conceptSet.First().ConceptKeys.Count);

            }
        }

        /// <summary>
        /// Test query concept and load via concepts
        /// </summary>
        [Test]
        public void TestQueryConceptLoadConcept()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var conceptSet = base.TestQuery<ConceptSet>(o => o.Mnemonic == "NullReason", 1).AsResultSet();
                Assert.AreEqual(15, conceptSet.First().ConceptKeys.Count);
                Assert.IsTrue(conceptSet.First().Concepts.Any(r => r.Key == NullReasonKeys.NoInformation));
            }
        }
    }
}