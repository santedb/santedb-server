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
    public class MaterialPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion with the proper persistence layer
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var material = new Material()
                {
                    DeterminerConceptKey = DeterminerKeys.Described,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Oral Polio Vaccine")
                    },
                    ExpiryDate = new DateTime(2021, 01, 01),
                    FormConceptKey = Guid.Parse("66cbce3a-2e77-401d-95d8-ee0361f4f076"), // Oral Drops
                    QuantityConceptKey = Guid.Parse("a4fc5c93-31c2-4f87-990e-c5a4e5ea2e76"), // dose
                    Quantity = 1,
                    IsAdministrative = true,
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
                Assert.IsTrue(afterInsert.IsAdministrative);
                Assert.AreEqual(Guid.Parse("66cbce3a-2e77-401d-95d8-ee0361f4f076"), afterInsert.FormConceptKey);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.FormConcept));
                Assert.IsTrue(afterInsert.FormConcept.Mnemonic.Contains("Oral"));
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.QuantityConcept));
                Assert.AreEqual(1, afterInsert.Quantity);

                // Now we want to query
                var afterQuery = base.TestQuery<Material>(o => o.Names.Any(n=>n.Component.Any(c=>c.Value == "Oral Polio Vaccine")) && o.IsAdministrative == true && o.FormConcept.Mnemonic == "AdministrableDrugForm-OralDrops" && o.QuantityConcept.Mnemonic == "UnitOfMeasure-Dose", 1).AsResultSet().First();
                Assert.AreEqual(new DateTime(2021, 01, 01), afterQuery.ExpiryDate);
                Assert.IsTrue(afterQuery.IsAdministrative);
                Assert.AreEqual(Guid.Parse("66cbce3a-2e77-401d-95d8-ee0361f4f076"), afterQuery.FormConceptKey);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.FormConcept));
                Assert.IsTrue(afterQuery.FormConcept.Mnemonic.Contains("Oral"));
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.QuantityConcept));
                Assert.AreEqual(1, afterQuery.Quantity);

                // Update the expiry and form
                var afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.ExpiryDate = new DateTime(2021, 07, 01);
                    o.FormConceptKey = Guid.Parse("af3a5fa5-7889-45f3-8809-2294129d49c8"); // otic drops
                    return o;
                });
                Assert.AreEqual(new DateTime(2021, 07, 01), afterUpdate.ExpiryDate);
                Assert.AreEqual(Guid.Parse("af3a5fa5-7889-45f3-8809-2294129d49c8"), afterUpdate.FormConceptKey);

                var date = new DateTime(2021, 01, 01);
                afterQuery = base.TestQuery<Material>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Oral Polio Vaccine")) && o.ExpiryDate >= date && o.FormConcept.Mnemonic == "AdministrableDrugForm-OralDrops", 0).AsResultSet().FirstOrDefault();
                afterQuery = base.TestQuery<Material>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Oral Polio Vaccine")) && o.ExpiryDate >= date && o.FormConcept.Mnemonic == "AdministrableDrugForm-OticDrops", 1).AsResultSet().FirstOrDefault();
                Assert.AreEqual(new DateTime(2021, 07, 01), afterQuery.ExpiryDate);
                Assert.IsTrue(afterQuery.IsAdministrative);
                Assert.AreEqual(Guid.Parse("af3a5fa5-7889-45f3-8809-2294129d49c8"), afterQuery.FormConceptKey);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.FormConcept));
                Assert.IsTrue(afterQuery.FormConcept.Mnemonic.Contains("Otic"));
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
                var material = new Material()
                {
                    DeterminerConceptKey = DeterminerKeys.Described,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "Inactivated Polio Vaccine")
                    },
                    ExpiryDate = new DateTime(2021, 01, 01),
                    FormConceptKey = Guid.Parse("9902267c-8f77-4233-bfd3-e6b068ab326a"), // Injection
                    QuantityConceptKey = Guid.Parse("a4fc5c93-31c2-4f87-990e-c5a4e5ea2e76"), // dose
                    Quantity = 1,
                    IsAdministrative = true
                };

                // Perform the insert
                var afterInsert = base.TestInsert<Entity>(material);
                Assert.IsInstanceOf<Material>(afterInsert);

                // Now we want to query
                var afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Inactivated Polio Vaccine")), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.IsInstanceOf<Material>(afterQuery);
                Assert.AreEqual(Guid.Parse("9902267c-8f77-4233-bfd3-e6b068ab326a"), (afterQuery as Material).FormConceptKey);

                // Update the key
                var afterUpdate = base.TestUpdate<Entity>(afterQuery, (o) =>
                {
                    (o as Material).ExpiryDate = new DateTime(2021, 07, 01);
                    return o;
                });
                Assert.AreEqual(new DateTime(2021, 07, 01), (afterUpdate as Material).ExpiryDate);

                afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "Inactivated Polio Vaccine")), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.IsInstanceOf<Material>(afterQuery);
                Assert.AreEqual(Guid.Parse("9902267c-8f77-4233-bfd3-e6b068ab326a"), (afterQuery as Material).FormConceptKey);
                Assert.AreEqual(new DateTime(2021, 07, 01), (afterQuery as Material).ExpiryDate);
            }
        }
    }
}