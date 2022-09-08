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
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Identifier type persistence
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class IdentifierTypePersistenceServiceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion of the identiifer type
        /// </summary>
        [Test]
        public void TestInsertIdentifierType()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var identifierType = new IdentifierType()
                {
                    ScopeConceptKey = EntityClassKeys.Patient,
                    TypeConceptKey = IdentifierTypeKeys.Bank
                };

                var afterInsert = base.TestInsert(identifierType);
                Assert.IsNull(afterInsert.ScopeConcept);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.ScopeConcept));

                identifierType = new IdentifierType()
                {
                    TypeConcept = new Concept()
                    {
                        Key = IdentifierTypeKeys.Bank,
                        Mnemonic = "IdentifierType-Bank"
                    },
                    ScopeConcept = new Concept()
                    {
                        Key = EntityClassKeys.Patient
                    }
                };

                afterInsert = base.TestInsert(identifierType);
                Assert.IsNull(afterInsert.ScopeConcept);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.ScopeConcept));
            }
        }
    }
}