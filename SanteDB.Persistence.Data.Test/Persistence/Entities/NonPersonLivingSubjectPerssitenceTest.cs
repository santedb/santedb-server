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
using SanteDB.Core.Model;
using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Persistence tests for non-person living subject (food, chemical substances, etc.)
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class NonPersonLivingSubjectPerssitenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Tests the NPLS persistence service
        /// </summary>
        [Test]
        public void TestWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var npls = new NonPersonLivingSubject()
                {
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Assigned, "A New Disease")
                    },
                    StrainKey = NullReasonKeys.NoInformation
                };

                var afterInsert = base.TestInsert(npls);
                Assert.IsInstanceOf<NonPersonLivingSubject>(afterInsert);

                var afterQuery = base.TestQuery<NonPersonLivingSubject>(o => o.Strain.Mnemonic == "NullFlavor-NoInformation", 1).AsResultSet().First();
                Assert.IsNull(afterQuery.Strain);
                Assert.AreEqual("NullFlavor-NoInformation", afterQuery.LoadProperty(o => o.Strain).Mnemonic);

                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.StrainKey = NullReasonKeys.UnEncoded;
                    o.Strain = null;
                    return o;
                });

                base.TestQuery<NonPersonLivingSubject>(o => o.Strain.Mnemonic == "NullFlavor-NoInformation", 0);
                base.TestQuery<NonPersonLivingSubject>(o => o.Strain.Mnemonic == "NullFlavor-UnEncoded", 1);

                // Test with entity persistence


            }
        }
    }
}
