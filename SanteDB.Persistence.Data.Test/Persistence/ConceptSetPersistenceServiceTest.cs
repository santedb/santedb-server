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
using SanteDB.Core.Model;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Test fixture
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class ConceptSetPersistenceServiceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test query concept
        /// </summary>
        [Test]
        public void TestQueryConcept()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var conceptSet = base.TestQuery<ConceptSet>(o => o.Mnemonic == "NullReason", 1).AsResultSet();
                Assert.AreEqual(15, conceptSet.First().ConceptsXml.Count);
            }
        }

        /// <summary>
        /// Test query concept and load via concepts
        /// </summary>
        [Test]
        public void TestQueryConceptLoadConcept()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var conceptSet = base.TestQuery<ConceptSet>(o => o.Mnemonic == "NullReason", 1).AsResultSet();
                Assert.AreEqual(15, conceptSet.First().ConceptsXml.Count);
                Assert.IsTrue(conceptSet.First().Concepts.Any(r => r.Key == NullReasonKeys.NoInformation));
            }
        }
    }
}