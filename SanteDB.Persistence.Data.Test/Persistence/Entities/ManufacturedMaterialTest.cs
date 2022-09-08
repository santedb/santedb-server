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
    public class ManufacturedMaterialPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion with the proper persistence layer
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var material = new ManufacturedMaterial()
                {
                    DeterminerConceptKey = DeterminerKeys.Described,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Dilbert Inc. Oral Polio Vaccine")
                    },
                    ExpiryDate = new DateTime(2021, 01, 01),
                    FormConceptKey = Guid.Parse("66cbce3a-2e77-401d-95d8-ee0361f4f076"), // Oral Drops
                    QuantityConceptKey = Guid.Parse("a4fc5c93-31c2-4f87-990e-c5a4e5ea2e76"), // dose
                    Quantity = 1,
                    IsAdministrative = true,
                    LotNumber = "1234-203",
                    Notes = new List<Core.Model.DataTypes.EntityNote>()
                    {
                        new Core.Model.DataTypes.EntityNote()
                        {
                            Text = "Don't administer to children > 4",
                            Author = new Organization()
                            {
                                Names = new List<EntityName>()
                                {
                                    new EntityName(NameUseKeys.Legal, "Someone")
                                }
                            }
                        }
                    }
                };

                // Perform the insert
                var afterInsert = base.TestInsert(material);
                Assert.AreEqual(new DateTime(2021, 01, 01), afterInsert.ExpiryDate);
                Assert.AreEqual("1234-203", afterInsert.LotNumber);
                Assert.IsTrue(afterInsert.IsAdministrative);
                Assert.AreEqual(Guid.Parse("66cbce3a-2e77-401d-95d8-ee0361f4f076"), afterInsert.FormConceptKey);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.FormConcept));
                Assert.IsTrue(afterInsert.FormConcept.Mnemonic.Contains("Oral"));
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.QuantityConcept));
                Assert.AreEqual(1, afterInsert.Quantity);

                // Now we want to query
                var afterQuery = base.TestQuery<ManufacturedMaterial>(o => o.IsAdministrative == true && o.LotNumber == "1234-203" && o.FormConcept.Mnemonic == "AdministrableDrugForm-OralDrops" && o.QuantityConcept.Mnemonic == "UnitOfMeasure-Dose", 1).AsResultSet().First();
                Assert.AreEqual(new DateTime(2021, 01, 01), afterQuery.ExpiryDate);
                Assert.IsTrue(afterQuery.IsAdministrative);
                Assert.AreEqual("1234-203", afterQuery.LotNumber);
                Assert.AreEqual(Guid.Parse("66cbce3a-2e77-401d-95d8-ee0361f4f076"), afterQuery.FormConceptKey);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.FormConcept));
                Assert.IsTrue(afterQuery.FormConcept.Mnemonic.Contains("Oral"));
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.QuantityConcept));
                Assert.AreEqual(1, afterQuery.Quantity);

                // Update the expiry and form
                var afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.LotNumber = "432-42";
                    return o;
                });
                Assert.AreEqual(Guid.Parse("66cbce3a-2e77-401d-95d8-ee0361f4f076"), afterUpdate.FormConceptKey);

                var date = new DateTime(2021, 01, 01);
                afterQuery = base.TestQuery<ManufacturedMaterial>(o => o.ExpiryDate >= date && o.LotNumber == "1234-203" && o.FormConcept.Mnemonic == "AdministrableDrugForm-OralDrops", 0).AsResultSet().FirstOrDefault();
                afterQuery = base.TestQuery<ManufacturedMaterial>(o => o.ExpiryDate >= date && o.LotNumber == "432-42" && o.FormConcept.Mnemonic == "AdministrableDrugForm-OralDrops", 1).AsResultSet().FirstOrDefault();
                Assert.IsTrue(afterQuery.IsAdministrative);
                Assert.AreEqual("432-42", afterQuery.LotNumber);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.FormConcept));
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.QuantityConcept));
                Assert.AreEqual(1, afterQuery.Quantity);
            }
        }

        /// <summary>
        /// Ensures that the generic Entity persistence service inserts the appropriate material data (i.e.
        /// it detects the presence of an material and inserts it)
        /// </summary>
        [Test]
        public void TestInsertWithImproper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var material = new ManufacturedMaterial()
                {
                    DeterminerConceptKey = DeterminerKeys.Described,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Dilbert Inc. Inactivated Polio Vaccine")
                    },
                    ExpiryDate = new DateTime(2021, 01, 01),
                    FormConceptKey = Guid.Parse("9902267c-8f77-4233-bfd3-e6b068ab326a"), // Injection
                    QuantityConceptKey = Guid.Parse("a4fc5c93-31c2-4f87-990e-c5a4e5ea2e76"), // dose
                    Quantity = 1,
                    LotNumber = "59494-40",
                    IsAdministrative = true
                };

                // Perform the insert
                var afterInsert = base.TestInsert<Entity>(material);
                Assert.IsInstanceOf<Material>(afterInsert);
                Assert.IsInstanceOf<ManufacturedMaterial>(afterInsert);

                // Now we want to query
                var afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Dilbert Inc. Inactivated Polio Vaccine")), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.IsInstanceOf<Material>(afterQuery);
                Assert.IsInstanceOf<ManufacturedMaterial>(afterQuery);
                Assert.AreEqual(Guid.Parse("9902267c-8f77-4233-bfd3-e6b068ab326a"), (afterQuery as Material).FormConceptKey);
                Assert.AreEqual("59494-40", (afterQuery as ManufacturedMaterial).LotNumber);

                // Update the key
                var afterUpdate = base.TestUpdate<Entity>(afterQuery, (o) =>
                {
                    (o as ManufacturedMaterial).LotNumber = "9999-99";
                    return o;
                });
                Assert.AreEqual("9999-99", (afterQuery as ManufacturedMaterial).LotNumber);

                // Query back out
                afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Dilbert Inc. Inactivated Polio Vaccine")), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.IsInstanceOf<Material>(afterQuery);
                Assert.IsInstanceOf<ManufacturedMaterial>(afterQuery);
                Assert.AreEqual(Guid.Parse("9902267c-8f77-4233-bfd3-e6b068ab326a"), (afterQuery as Material).FormConceptKey);
                Assert.AreEqual("9999-99", (afterQuery as ManufacturedMaterial).LotNumber);

                // Query back out with Material 
                var afterMatQuery = base.TestQuery<Material>(o=> o.Names.Any(n => n.Component.Any(c => c.Value == "Dilbert Inc. Inactivated Polio Vaccine")), 1).AsResultSet().First();
                Assert.IsNotNull(afterMatQuery);
                Assert.AreEqual(Guid.Parse("9902267c-8f77-4233-bfd3-e6b068ab326a"), afterMatQuery.FormConceptKey);
                Assert.AreEqual("9999-99", (afterMatQuery as ManufacturedMaterial).LotNumber);
            }
        }
    }
}