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
    /// Tests for Organization
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class OrganizationPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Test insertion with the proper persistence layer
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var organization = new Organization()
                {
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "123 Organization Inc.")
                    },
                    IndustryConceptKey = IndustryTypeKeys.Manufacturing
                };

                // Perform the insert
                var afterInsert = base.TestInsert(organization);
                Assert.AreEqual(IndustryTypeKeys.Manufacturing, afterInsert.IndustryConceptKey);

                // Now we want to query
                var afterQuery = base.TestQuery<Organization>(o => o.IndustryConceptKey == IndustryTypeKeys.Manufacturing && o.Names.Any(n => n.Component.Any((c => c.Value == "123 Organization Inc."))), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual(IndustryTypeKeys.Manufacturing, afterQuery.IndustryConceptKey);
                Assert.AreEqual("Industry-Manufacturing", afterQuery.LoadProperty(o => o.IndustryConcept).Mnemonic);

                // Update the key
                var afterUpdate = base.TestUpdate(afterQuery, (o) =>
                {
                    o.IndustryConceptKey = IndustryTypeKeys.HealthDelivery;
                    return o;
                });
                Assert.AreEqual(IndustryTypeKeys.HealthDelivery, afterUpdate.IndustryConceptKey);

                afterQuery = base.TestQuery<Organization>(o => o.IndustryConceptKey == IndustryTypeKeys.HealthDelivery && o.Names.Any(n => n.Component.Any(c => c.Value == "123 Organization Inc.")), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual(IndustryTypeKeys.HealthDelivery, afterQuery.IndustryConceptKey);
                Assert.AreEqual("Industry-HealthDelivery", afterQuery.LoadProperty(o => o.IndustryConcept).Mnemonic);
            }
        }

        /// <summary>
        /// Ensures that the generic Entity persistence service inserts the appropriate organization data (i.e.
        /// it detects the presence of an organization and inserts it)
        /// </summary>
        [Test]
        public void TestInsertWithImproper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var organization = new Organization()
                {
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "324 Organization Inc.")
                    },
                    IndustryConceptKey = IndustryTypeKeys.Manufacturing
                };

                // Perform the insert
                var afterInsert = base.TestInsert<Entity>(organization) as Organization;
                Assert.IsNotNull(afterInsert);
                Assert.AreEqual(IndustryTypeKeys.Manufacturing, afterInsert.IndustryConceptKey);

                // Now we want to query
                var existingOrgIds = new Guid[]
                {
                    Guid.Parse("ee4029cd-479b-43c5-ab96-183b9236d47b")
                };
                var afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "324 Organization Inc.")), 1).AsResultSet().First();
                Assert.IsInstanceOf<Organization>(afterQuery);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual(IndustryTypeKeys.Manufacturing, (afterQuery as Organization).IndustryConceptKey);

                // Update the key
                var afterUpdate = base.TestUpdate<Entity>(afterQuery, (o) =>
                {
                    (o as Organization).IndustryConceptKey = IndustryTypeKeys.HealthDelivery;
                    return o;
                }) as Organization;
                Assert.IsNotNull(afterUpdate);
                Assert.AreEqual(IndustryTypeKeys.HealthDelivery, afterUpdate.IndustryConceptKey);

                afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "324 Organization Inc.")), 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.Names).Count);
                Assert.AreEqual(IndustryTypeKeys.HealthDelivery, (afterQuery as Organization).IndustryConceptKey);
            }
        }

        /// <summary>
        /// Test retrieve with improper provider
        /// </summary>
        [Test]
        public void TestRetreiveImproper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var organization = new Organization()
                {
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "999 Organization Inc.")
                    },
                    IndustryConceptKey = IndustryTypeKeys.Manufacturing
                };

                // Perform the insert
                var afterInsert = base.TestInsert<Entity>(organization) as Organization;
                Assert.AreEqual(IndustryTypeKeys.Manufacturing, afterInsert.IndustryConceptKey);

                var afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Any(c => c.Value == "999 Organization Inc.")), 1);
                Assert.IsInstanceOf<Organization>(afterQuery.First());
                Assert.AreEqual(IndustryTypeKeys.Manufacturing, afterQuery.OfType<Organization>().First().IndustryConceptKey);
            }
        }
    }
}