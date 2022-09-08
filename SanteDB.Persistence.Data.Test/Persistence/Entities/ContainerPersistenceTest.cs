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
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Test for persistence of materials
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class ContainerPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion with the proper persistence layer
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var container = new Container()
                {
                    DeterminerConceptKey = DeterminerKeys.Described,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "OPV 100 Quantity")
                    },
                    QuantityConceptKey = Guid.Parse("a4fc5c93-31c2-4f87-990e-c5a4e5ea2e76"), // dose
                    CapacityQuantity = 10,
                    DiameterQuantity = 5,
                    HeightQuantity = 10
                };

                // Perform the insert
                var afterInsert = base.TestInsert(container);
                Assert.IsNull(afterInsert.QuantityConcept);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.QuantityConcept));
                Assert.AreEqual(10, afterInsert.CapacityQuantity);
                Assert.AreEqual(10, afterInsert.HeightQuantity);
                Assert.AreEqual(5, afterInsert.DiameterQuantity);

                // Now we want to query
                var afterQuery = base.TestQuery<Container>(o => o.Names.Any(n=>n.Component.Any(c=>c.Value == "OPV 100 Quantity")) && o.CapacityQuantity == 10, 1).AsResultSet().First();
                Assert.IsNull(afterQuery.QuantityConcept);
                Assert.AreEqual(10, afterQuery.CapacityQuantity);
                Assert.AreEqual(10, afterQuery.HeightQuantity);
                Assert.AreEqual(5, afterQuery.DiameterQuantity);

                // Update the expiry and form
                var afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.DiameterQuantity = 10;
                    o.HeightQuantity = 20;
                    o.CapacityQuantity = 20;
                    return o;
                });
                afterQuery = base.TestQuery<Container>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "OPV 100 Quantity")) && o.CapacityQuantity == 10, 0).AsResultSet().FirstOrDefault();
                afterQuery = base.TestQuery<Container>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "OPV 100 Quantity")) && o.CapacityQuantity == 20, 1).AsResultSet().FirstOrDefault();
                
                Assert.IsNull(afterQuery.QuantityConcept);
                Assert.AreEqual(20, afterQuery.CapacityQuantity);
                Assert.AreEqual(20, afterQuery.HeightQuantity);
                Assert.AreEqual(10, afterQuery.DiameterQuantity);

            }
        }

    }
}