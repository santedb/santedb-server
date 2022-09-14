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
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model.Entities;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// Patient persistence tests
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class PatientPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Get or create AA
        /// </summary>
        private Guid GetOrCreateAA()
        {
            var aaService = ApplicationServiceContext.Current.GetService<IIdentityDomainRepositoryService>();
            var aa = aaService.Get("HIN");
            if (aa == null)
            {
                aa = aaService.Insert(new IdentityDomain("HIN", "Health Identification Number", "2.25.303030") { Url = "http://hin.test" });
            }
            return aa.Key.Value;
        }

        /// <summary>
        /// Ensures that the persistence layer can insert a basic patient, query back the data, etc.
        /// </summary>
        [Test]
        public void TestInsertWithProperSimple()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var patient = new Patient()
                {
                    Addresses = new List<Core.Model.Entities.EntityAddress>()
                    {
                        new Core.Model.Entities.EntityAddress(AddressUseKeys.HomeAddress, "123 East West St.", "Toronto", "ON", "CA", "L8K5N2")
                    },
                    DateOfBirth = new DateTime(1982, 04, 14),
                    GenderConceptKey = NullReasonKeys.Other,
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new EntityIdentifier(this.GetOrCreateAA(), "1234-1")
                    },
                    Names = new List<Core.Model.Entities.EntityName>()
                    {
                        new Core.Model.Entities.EntityName(NameUseKeys.OfficialRecord, "Smith","John")
                    },
                    Telecoms = new List<Core.Model.Entities.EntityTelecomAddress>()
                    {
                        new Core.Model.Entities.EntityTelecomAddress(TelecomAddressUseKeys.AnsweringService, "123-123-1234") { TypeConceptKey = TelecomAddressTypeKeys.Telephone }
                    },
                    MaritalStatusKey = NullReasonKeys.NotAsked
                };

                var afterInsert = base.TestInsert(patient);
                Assert.IsNotNull(afterInsert.VersionKey);

                // Attempt to query 
                var afterQuery = base.TestQuery<Patient>(o => o.Names.Any(n => n.Component.Where(g => g.ComponentType.Mnemonic == "Given").Any(c => c.Value == "John")) && o.DateOfBirth <= DateTime.Now && o.Telecoms.Any(t => t.Value == "123-123-1234"), 1).AsResultSet().First();
                Assert.AreEqual(afterInsert.Key, afterQuery.Key);
                Assert.IsNull(afterQuery.GenderConcept);
                Assert.AreEqual("NullFlavor-Other", afterQuery.LoadProperty(o => o.GenderConcept).Mnemonic);
                Assert.AreEqual(this.GetOrCreateAA(), afterQuery.LoadProperty(o => o.Identifiers).First().IdentityDomainKey);

                // Attempt query by marital status (special patient property)
                afterQuery = base.TestQuery<Patient>(o => o.MaritalStatusKey == NullReasonKeys.NotAsked, 1).AsResultSet().First();

                var divorced = Guid.Parse("9f0a6c4c-efdb-4ad2-9d54-7b21f9da0bf5");

                // Attempt to update
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.MaritalStatusKey = divorced; // separated
                    return o;
                });
                Assert.AreEqual(divorced, afterUpdate.MaritalStatusKey);

                base.TestQuery<Patient>(o => o.MaritalStatusKey == NullReasonKeys.NotAsked, 0);
                afterQuery = base.TestQuery<Patient>(o => o.MaritalStatus.Mnemonic == "MaritalStatus-Divorced", 1).AsResultSet().First();
                Assert.AreEqual(afterInsert.Key, afterQuery.Key);
                Assert.AreEqual(divorced, afterQuery.MaritalStatusKey);

            }
        }

        /// <summary>
        /// Ensures that query result context settings are adhered to
        /// </summary>
        [Test]
        public void TestQueryResultContextSetting()
        {
            using (AuthenticationContext.EnterSystemContext())
            {

                var commonlaw = Guid.Parse("035f29e4-f1b6-4afa-b436-64051d2cddb6");
                var agnostic = Guid.Parse("18d758d9-956e-4a3f-a8fa-3b7b6b736515");
                var boardMember = Guid.Parse("2f5b021c-7ad1-11eb-9439-0242ac130002");
                var hispanic = Guid.Parse("c4418973-c7c5-4485-bf2f-48062ef641db");
                var alone = Guid.Parse("f3df665f-2600-4dbf-a555-f62c5ff33c46");
                var canadian = Guid.Parse("919439CA-6983-11EC-90D6-0242AC120003");

                var patient = new Patient()
                {
                    Addresses = new List<Core.Model.Entities.EntityAddress>()
                    {
                        new Core.Model.Entities.EntityAddress(AddressUseKeys.HomeAddress, "123 West St. W", "Toronto", "ON", "CA", "L8K5N2")
                    },
                    DateOfBirth = new DateTime(1982, 04, 14),
                    GenderConceptKey = NullReasonKeys.Other,
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new EntityIdentifier(this.GetOrCreateAA(), "1234-2")
                    },
                    Names = new List<Core.Model.Entities.EntityName>()
                    {
                        new Core.Model.Entities.EntityName(NameUseKeys.OfficialRecord, "Hernandez", "Jose")
                    },
                    Telecoms = new List<Core.Model.Entities.EntityTelecomAddress>()
                    {
                        new Core.Model.Entities.EntityTelecomAddress(TelecomAddressUseKeys.AnsweringService, "123-123-4321") { TypeConceptKey = TelecomAddressTypeKeys.Telephone }
                    },
                    MaritalStatusKey = commonlaw,
                    ReligiousAffiliationKey = agnostic,
                    VipStatusKey = boardMember,
                    EthnicGroupKey = hispanic,
                    LivingArrangementKey = alone,
                    MultipleBirthOrder = 1,
                    NationalityKey = canadian
                };

                var afterInsert = base.TestInsert(patient);
                Assert.IsNotNull(afterInsert.VersionKey);

                ApplicationServiceContext.Current.GetService<IDataCachingService>().Clear();

                // Attempt to query 
                var afterQuery = base.TestQuery<Patient>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual(afterInsert.Key, afterQuery.Key);

                // Default for this context, no properties are loaded
                Assert.IsNull(afterQuery.GenderConcept);
                Assert.IsNull(afterQuery.MaritalStatus);
                Assert.IsNull(afterQuery.ReligiousAffiliation);
                Assert.IsNull(afterQuery.LivingArrangement);
                Assert.IsNull(afterQuery.EthnicGroup);
                Assert.IsNull(afterQuery.Nationality);

                // Sync properties should not be loaded (we're quick load by default)
                Assert.IsNull(afterQuery.Identifiers);
                Assert.IsNull(afterQuery.Names);
                Assert.IsNull(afterQuery.Addresses);
                Assert.IsNull(afterQuery.Telecoms);

                // Enter a custom query context
                using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                {
                    ApplicationServiceContext.Current.GetService<IDataCachingService>().Clear();

                    afterQuery = base.TestQuery<Patient>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                    Assert.AreEqual(afterInsert.Key, afterQuery.Key);

                    // No direct properties are loaded (in sync mode so UUID is for sync)
                    Assert.IsNull(afterQuery.GenderConcept);
                    Assert.IsNull(afterQuery.MaritalStatus);
                    Assert.IsNull(afterQuery.ReligiousAffiliation);
                    Assert.IsNull(afterQuery.LivingArrangement);
                    Assert.IsNull(afterQuery.EthnicGroup);
                    Assert.IsNull(afterQuery.Nationality);

                    // Sync properties should be loaded 
                    Assert.IsNotNull(afterQuery.Identifiers);
                    Assert.IsNotNull(afterQuery.Names);
                    Assert.IsNotNull(afterQuery.Addresses);
                    Assert.IsNotNull(afterQuery.Telecoms);
                }

                // Enter a custom query context
                using (DataPersistenceControlContext.Create(LoadMode.FullLoad))
                {
                    ApplicationServiceContext.Current.GetService<IDataCachingService>().Clear();

                    afterQuery = base.TestQuery<Patient>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                    Assert.AreEqual(afterInsert.Key, afterQuery.Key);

                    // Direct properties are loaded 
                    Assert.IsNotNull(afterQuery.GenderConcept);
                    Assert.IsNotNull(afterQuery.MaritalStatus);
                    Assert.IsNotNull(afterQuery.ReligiousAffiliation);
                    Assert.IsNotNull(afterQuery.LivingArrangement);
                    Assert.IsNotNull(afterQuery.EthnicGroup);
                    Assert.IsNotNull(afterQuery.Nationality);

                    // Sync properties should be loaded 
                    Assert.IsNotNull(afterQuery.Identifiers);
                    Assert.IsNotNull(afterQuery.Names);
                    Assert.IsNotNull(afterQuery.Addresses);
                    Assert.IsNotNull(afterQuery.Telecoms);
                }

            }

        }
        /// <summary>
        /// Ensures the persistence layer can insert a complex patient, query back the data, etc.
        /// </summary>
        [Test]
        public void TestInsertWithProperComplex()
        {
            using (AuthenticationContext.EnterSystemContext())
            {

                var commonlaw = Guid.Parse("035f29e4-f1b6-4afa-b436-64051d2cddb6");
                var agnostic = Guid.Parse("18d758d9-956e-4a3f-a8fa-3b7b6b736515");
                var boardMember = Guid.Parse("2f5b021c-7ad1-11eb-9439-0242ac130002");
                var hispanic = Guid.Parse("c4418973-c7c5-4485-bf2f-48062ef641db");
                var alone = Guid.Parse("f3df665f-2600-4dbf-a555-f62c5ff33c46");
                var canadian = Guid.Parse("919439CA-6983-11EC-90D6-0242AC120003");

                var patient = new Patient()
                {
                    Addresses = new List<Core.Model.Entities.EntityAddress>()
                    {
                        new Core.Model.Entities.EntityAddress(AddressUseKeys.HomeAddress, "123 West St. W", "Toronto", "ON", "CA", "L8K5N2")
                    },
                    DateOfBirth = new DateTime(1982, 04, 14),
                    GenderConceptKey = NullReasonKeys.Other,
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new EntityIdentifier(this.GetOrCreateAA(), "1234-3")
                    },
                    Names = new List<Core.Model.Entities.EntityName>()
                    {
                        new Core.Model.Entities.EntityName(NameUseKeys.OfficialRecord, "Jones", "Jennifer")
                    },
                    Telecoms = new List<Core.Model.Entities.EntityTelecomAddress>()
                    {
                        new Core.Model.Entities.EntityTelecomAddress(TelecomAddressUseKeys.AnsweringService, "123-123-4321") { TypeConceptKey = TelecomAddressTypeKeys.Telephone }
                    },
                    MaritalStatusKey = commonlaw,
                    ReligiousAffiliationKey = agnostic,
                    VipStatusKey = boardMember,
                    EthnicGroupKey = hispanic,
                    LivingArrangementKey = alone,
                    MultipleBirthOrder = 3,
                    NationalityKey = canadian
                };

                var afterInsert = base.TestInsert(patient);
                Assert.IsNotNull(afterInsert.VersionKey);

                base.TestQuery<Patient>(o => o.MultipleBirthOrder == 3, 1);
                base.TestQuery<Patient>(o => o.MultipleBirthOrder == 2, 0);
                base.TestQuery<Patient>(o => o.MaritalStatusKey == commonlaw, 1);
                base.TestQuery<Patient>(o => o.MaritalStatus.Mnemonic == "MaritalStatus-Married", 0);
                base.TestQuery<Patient>(o => o.MaritalStatus.Mnemonic == "MaritalStatus-CommonLaw", 1);
                base.TestQuery<Patient>(o => o.VipStatusKey == boardMember, 1);
                base.TestQuery<Patient>(o => o.VipStatus.Mnemonic == "VIPStatus-BoardMember", 1);
                base.TestQuery<Patient>(o => o.EthnicGroup.Mnemonic == "ETHNICITY-HispanicLatino", 1);
                base.TestQuery<Patient>(o => o.NationalityKey == canadian, 1);
                base.TestQuery<Patient>(o => o.ReligiousAffiliationKey == agnostic, 1);

                // Attempt to update
                var afterUpdate = base.TestUpdate(afterInsert, o =>
                {
                    o.MultipleBirthOrder = 4;
                    o.DeceasedDate = DateTime.Now;
                    o.DeceasedDatePrecision = DatePrecision.Day;
                    o.VipStatusKey = null;
                    o.EthnicGroupKey = null;
                    o.MaritalStatusKey = NullReasonKeys.Masked;
                    o.ReligiousAffiliationKey = NullReasonKeys.Masked;
                    return o;
                });

                base.TestQuery<Patient>(o => o.MultipleBirthOrder == 3, 0);
                base.TestQuery<Patient>(o => o.MultipleBirthOrder == 4, 1);
                base.TestQuery<Patient>(o => o.MaritalStatusKey == commonlaw, 0);
                base.TestQuery<Patient>(o => o.MaritalStatus.Mnemonic == "NullFlavor-Masked", 1);
                base.TestQuery<Patient>(o => o.VipStatusKey == boardMember, 0);
                base.TestQuery<Patient>(o => o.VipStatus.Mnemonic == "VIPStatus-BoardMember", 0);
                base.TestQuery<Patient>(o => o.EthnicGroup.Mnemonic == "ETHNICITY-HispanicLatino", 0);
                base.TestQuery<Patient>(o => o.NationalityKey == canadian, 1);
                base.TestQuery<Patient>(o => o.ReligiousAffiliationKey == agnostic, 0);
                base.TestQuery<Patient>(o => o.ReligiousAffiliationKey == NullReasonKeys.Masked, 1);

            }
        }

        /// <summary>
        /// Ensures that the Entity persistence service can load Patient objects
        /// </summary>
        [Test]
        public void TestInsertWithImproperEntity()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var patient = new Patient()
                {
                    Addresses = new List<Core.Model.Entities.EntityAddress>()
                    {
                        new Core.Model.Entities.EntityAddress(AddressUseKeys.HomeAddress, "123 West St. E", "Toronto", "ON", "CA", "L8K5N2")
                    },
                    DateOfBirth = new DateTime(1998, 04, 14),
                    GenderConceptKey = NullReasonKeys.Other,
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new EntityIdentifier(this.GetOrCreateAA(), "1234-4")
                    },
                    Names = new List<Core.Model.Entities.EntityName>()
                    {
                        new Core.Model.Entities.EntityName(NameUseKeys.OfficialRecord, "Smith","Entity1")
                    },
                    MaritalStatusKey = NullReasonKeys.NotAsked
                };

                var afterInsert = base.TestInsert<Entity>(patient);
                Assert.IsNotNull(afterInsert.VersionKey);
                Assert.IsInstanceOf<Patient>(afterInsert);

                // Attempt to query 
                var afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Where(g => g.ComponentType.Mnemonic == "Given").Any(c => c.Value == "Entity1")), 1).AsResultSet().First();
                Assert.AreEqual(afterInsert.Key, afterQuery.Key);
                Assert.IsInstanceOf<Patient>(afterQuery);
                Assert.AreEqual(this.GetOrCreateAA(), afterQuery.LoadProperty(o => o.Identifiers).First().IdentityDomainKey);
                Assert.AreEqual(NullReasonKeys.NotAsked, (afterQuery as Patient).MaritalStatusKey);

                var separated = Guid.Parse("fcba6282-a886-444d-b534-c5dc263e2539");

                // Attempt to update
                var afterUpdate = base.TestUpdate<Entity>(afterQuery, o =>
                {
                    (o as Patient).MaritalStatusKey = separated; // separated
                    return o;
                });

                afterQuery = base.TestQuery<Entity>(o => o.Names.Any(n => n.Component.Where(g => g.ComponentType.Mnemonic == "Given").Any(c => c.Value == "Entity1")), 1).AsResultSet().First();
                Assert.IsInstanceOf<Patient>(afterQuery);
                Assert.AreEqual(this.GetOrCreateAA(), afterQuery.LoadProperty(o => o.Identifiers).First().IdentityDomainKey);
                Assert.AreEqual(separated, (afterQuery as Patient).MaritalStatusKey);

            }
        }

        /// <summary>
        /// Ensures that the Person persistence service can load Patient objects
        /// </summary>
        [Test]
        public void TestInsertWithImproperPerson()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var patient = new Patient()
                {
                    Addresses = new List<Core.Model.Entities.EntityAddress>()
                    {
                        new Core.Model.Entities.EntityAddress(AddressUseKeys.HomeAddress, "123 East St. W", "Toronto", "ON", "CA", "L8K5N2")
                    },
                    DateOfBirth = new DateTime(1992, 04, 14),
                    GenderConceptKey = NullReasonKeys.Other,
                    Identifiers = new List<Core.Model.DataTypes.EntityIdentifier>()
                    {
                        new EntityIdentifier(this.GetOrCreateAA(), "1234-4")
                    },
                    Names = new List<Core.Model.Entities.EntityName>()
                    {
                        new Core.Model.Entities.EntityName(NameUseKeys.OfficialRecord, "Smith","Person1")
                    },
                    MaritalStatusKey = NullReasonKeys.NotAsked
                };

                var afterInsert = base.TestInsert<Person>(patient);
                Assert.IsNotNull(afterInsert.VersionKey);
                Assert.IsInstanceOf<Patient>(afterInsert);

                // Attempt to query 
                var afterQuery = base.TestQuery<Person>(o => o.Names.Any(n => n.Component.Where(g => g.ComponentType.Mnemonic == "Given").Any(c => c.Value == "Person1")) && o.DateOfBirth == patient.DateOfBirth, 1).AsResultSet().First();
                Assert.AreEqual(afterInsert.Key, afterQuery.Key);
                Assert.IsInstanceOf<Patient>(afterQuery);
                Assert.AreEqual(this.GetOrCreateAA(), afterQuery.LoadProperty(o => o.Identifiers).First().IdentityDomainKey);
                Assert.AreEqual(NullReasonKeys.NotAsked, (afterQuery as Patient).MaritalStatusKey);

                var separated = Guid.Parse("fcba6282-a886-444d-b534-c5dc263e2539");

                // Attempt to update
                var afterUpdate = base.TestUpdate<Person>(afterQuery, o =>
                {
                    (o as Patient).MaritalStatusKey = separated; // separated
                    return o;
                });

                afterQuery = base.TestQuery<Person>(o => o.Names.Any(n => n.Component.Where(g => g.ComponentType.Mnemonic == "Given").Any(c => c.Value == "Person1")) && o.DateOfBirth == patient.DateOfBirth, 1).AsResultSet().First();
                Assert.IsInstanceOf<Patient>(afterQuery);
                Assert.AreEqual(this.GetOrCreateAA(), afterQuery.LoadProperty(o => o.Identifiers).First().IdentityDomainKey);
                Assert.AreEqual(separated, (afterQuery as Patient).MaritalStatusKey);

            }
        }
    }
}
