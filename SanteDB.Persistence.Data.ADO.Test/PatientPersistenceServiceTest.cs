/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Tests
{
    /// <summary>
    /// Test class for patient persistence
    /// </summary>
    [TestFixture(Category = "Persistence")]
    public class PatientPersistenceServiceTest : PersistenceTest<Patient>
    {
        private static IPrincipal s_authorization;
        [SetUp]
        public void ClassSetup()
        {
            s_authorization = AuthenticationContext.SystemPrincipal;

        }
        /// <summary>
        /// Test the persistence of a person
        /// </summary>
        [Test]
        public void TestPersistPatient()
        {

            Patient p = new Patient()
            {
                StatusConceptKey = StatusKeys.Active,
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.OfficialRecord, "Johnson", "William", "P.", "Bear")
                },
                Addresses = new List<EntityAddress>()
                {
                    new EntityAddress(AddressUseKeys.HomeAddress, "123 Main Street West", "Hamilton", "ON", "CA", "L8K5N2")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority() { Name = "OHIPCARD12", DomainName = "OHIPCARD12", Oid = "1.2.3.4.5.6" }, "12343120423")
                },
                Telecoms = new List<EntityTelecomAddress>()
                {
                    new EntityTelecomAddress(AddressUseKeys.WorkPlace, "mailto:will@johnson.com")
                },
                Tags = new List<EntityTag>()
                {
                    new EntityTag("hasBirthCertificate", "true")
                },
                Notes = new List<EntityNote>()
                {
                    new EntityNote(Guid.Empty, "William is a test patient")
                    {
                        Author = new Person()
                    }
                },
                Extensions = new List<EntityExtension>() {
                    new EntityExtension()
                    {
                        ExtensionType = new ExtensionType()
                        {
                            Name = "http://santedb.org/oiz/birthcertificate",
                            ExtensionHandler = typeof(EntityPersistenceServiceTest)
                        },
                        ExtensionValueXml = new byte[] { 1 }
                    }
                },
                GenderConceptKey = Guid.Parse("f4e3a6bb-612e-46b2-9f77-ff844d971198"),
                DateOfBirth = new DateTime(1984, 03, 22),
                MultipleBirthOrder = 2,
                DeceasedDate = new DateTime(2016,05,02),
                DeceasedDatePrecision = DatePrecision.Day,
                DateOfBirthPrecision = DatePrecision.Day
            };

            Person mother = new Person()
            {
                StatusConceptKey = StatusKeys.Active,
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Johnson", "Martha")
                }
            };

            // Associate: PARENT > CHILD
            p.Relationships.Add(new EntityRelationship(EntityRelationshipTypeKeys.Mother, mother));

            var afterInsert = base.DoTestInsert(p, s_authorization);
            Assert.AreEqual(DatePrecision.Day, afterInsert.DateOfBirthPrecision);
            Assert.AreEqual(DatePrecision.Day, afterInsert.DeceasedDatePrecision);
            Assert.AreEqual(new DateTime(1984, 03, 22), afterInsert.DateOfBirth);
            Assert.AreEqual(new DateTime(2016, 05, 02), afterInsert.DeceasedDate);
            Assert.AreEqual("Male", afterInsert.GenderConcept.Mnemonic);
            Assert.AreEqual(2, afterInsert.MultipleBirthOrder);
            Assert.AreEqual(1, p.Names.Count);
            Assert.AreEqual(1, p.Addresses.Count);
            Assert.AreEqual(1, p.Identifiers.Count);
            Assert.AreEqual(1, p.Telecoms.Count);
            Assert.AreEqual(1, p.Tags.Count);
            Assert.AreEqual(1, p.Notes.Count);
            Assert.AreEqual(EntityClassKeys.Patient, p.ClassConceptKey);
            Assert.AreEqual(DeterminerKeys.Specific, p.DeterminerConceptKey);
            Assert.AreEqual(StatusKeys.Active, p.StatusConceptKey);


        }

        /// <summary>
        /// Test the persistence of a person
        /// </summary>
        [Test]
        public void TestShouldAdhereToClassifierCodes()
        {
            AssigningAuthority aa = new AssigningAuthority()
            {
                Name = "Ontario Health Insurance Card",
                DomainName = "OHIPCARD",
                Oid = "1.2.3.4.5.67"
            };
            var aaPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<AssigningAuthority>>();
            var ohipAuth = aaPersistence.Insert(aa, TransactionMode.Commit, s_authorization);

            Patient p = new Patient()
            {
                StatusConcept = new Concept()
                {
                    Mnemonic = "ACTIVE"
                },
                Names = new List<EntityName>()
                {
                    new EntityName() {
                        NameUse = new Concept() { Mnemonic = "OfficialRecord" },
                        Component = new List<EntityNameComponent>() {
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Family" },
                                Value = "Johnson"
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "William"
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "P."
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "Bear"
                            }
                        }
                    }
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(
                        new AssigningAuthority() { DomainName = "OHIPCARD" }, "12343120423")
                },
                Tags = new List<EntityTag>()
                {
                    new EntityTag("hasBirthCertificate", "true")
                },
                Extensions = new List<EntityExtension>() {
                    new EntityExtension()
                    {
                        ExtensionType = new ExtensionType()
                        {
                            Name = "http://santedb.org/oiz/birthcertificate",
                            ExtensionHandler = typeof(EntityPersistenceServiceTest)
                        },
                        ExtensionValueXml = new byte[] { 1 }
                    }
                },
                GenderConcept = new Concept() {  Mnemonic = "Male" },
                DateOfBirth = new DateTime(1984, 03, 22),
                MultipleBirthOrder = 2,
                DeceasedDate = new DateTime(2016, 05, 02),
                DeceasedDatePrecision = DatePrecision.Day,
                DateOfBirthPrecision = DatePrecision.Day
            };

            var afterInsert = base.DoTestInsert(p, s_authorization);
            Assert.AreEqual("Male", afterInsert.GenderConcept.Mnemonic);
            Assert.AreEqual(EntityClassKeys.Patient, p.ClassConceptKey);
            Assert.AreEqual(DeterminerKeys.Specific, p.DeterminerConceptKey);
            Assert.AreEqual(StatusKeys.Active, p.StatusConceptKey);
            //Assert.AreEqual(aa.Key, afterInsert.Identifiers[0].AuthorityKey);

        }

        /// <summary>
        /// Test the persistence of a person
        /// </summary>
        [Test]
        public void TestQueryByGender()
        {
            Patient p = new Patient()
            {
                StatusConcept = new Concept()
                {
                    Mnemonic = "ACTIVE"
                },
                Names = new List<EntityName>()
                {
                    new EntityName() {
                        NameUse = new Concept() { Mnemonic = "OfficialRecord" },
                        Component = new List<EntityNameComponent>() {
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "Allison"
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "P."
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Family" },
                                Value = "Bear"
                            }
                        }
                    }
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(
                        new AssigningAuthority() { DomainName = "OHIPCARD" }, "3242342323")
                },
                Tags = new List<EntityTag>()
                {
                    new EntityTag("hasBirthCertificate", "false")
                },
                Extensions = new List<EntityExtension>() {
                    new EntityExtension()
                    {
                        ExtensionType = new ExtensionType()
                        {
                            Name = "http://santedb.org/oiz/birthcertificate",
                            ExtensionHandler = typeof(EntityPersistenceServiceTest)
                        },
                        ExtensionValueXml = BitConverter.GetBytes(true)
                    }
                },
                GenderConcept = new Concept() { Mnemonic = "Female" },
                DateOfBirth = new DateTime(1990, 03, 22),
                MultipleBirthOrder = 2,
                DateOfBirthPrecision = DatePrecision.Day
            };

            var afterInsert = base.DoTestInsert(p, s_authorization);

            var result = base.DoTestQuery(o => o.GenderConcept.Mnemonic == "Female", afterInsert.Key, s_authorization);
            
        }

        /// <summary>
        /// Test that the SQL per
        /// </summary>
        [Test]
        public void TestModelQueryIdentifierClassifier()
        {
            Patient p = new Patient()
            {
                StatusConcept = new Concept()
                {
                    Mnemonic = "ACTIVE"
                },
                Names = new List<EntityName>()
                {
                    new EntityName() {
                        NameUse = new Concept() { Mnemonic = "OfficialRecord" },
                        Component = new List<EntityNameComponent>() {
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "Allison"
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "P."
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Family" },
                                Value = "Bear"
                            }
                        }
                    }
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(
                        new AssigningAuthority() { DomainName = "OHIPCARD", Name = "OHIPCARD", Oid ="1.2.3.4.5" }, "102920")
                },
                Tags = new List<EntityTag>()
                {
                    new EntityTag("hasBirthCertificate", "false")
                },
                Extensions = new List<EntityExtension>() {
                    new EntityExtension()
                    {
                        ExtensionType = new ExtensionType()
                        {
                            Name = "http://santedb.org/oiz/birthcertificate",
                            ExtensionHandler = typeof(EntityPersistenceServiceTest)
                        },
                        ExtensionValueXml = BitConverter.GetBytes(true)
                    }
                },
                GenderConcept = new Concept() { Mnemonic = "Female" },
                DateOfBirth = new DateTime(1990, 03, 22),
                MultipleBirthOrder = 2,
                DateOfBirthPrecision = DatePrecision.Day
            };

            var afterInsert = base.DoTestInsert(p, s_authorization);

            var result = base.DoTestQuery(o => o.Identifiers.Where(guard => guard.Authority.DomainName == "OHIPCARD").Any(identifier => identifier.Value == "102920"), afterInsert.Key, s_authorization);

            var authKey = afterInsert.Identifiers.First().AuthorityKey;

            result = base.DoTestQuery(o => o.Identifiers.Any(i => i.Authority.Key == authKey && i.Value == "102920"), afterInsert.Key, s_authorization);


        }

        /// <summary>
        /// Test the persistence of a person
        /// </summary>
        [Test]
        public void TestQueryByCreationDate()
        {
            Patient p = new Patient()
            {
                StatusConcept = new Concept()
                {
                    Mnemonic = "ACTIVE"
                },
                Names = new List<EntityName>()
                {
                    new EntityName() {
                        NameUse = new Concept() { Mnemonic = "OfficialRecord" },
                        Component = new List<EntityNameComponent>() {
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "Jamie"
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Given" },
                                Value = "A."
                            },
                            new EntityNameComponent() {
                                ComponentType = new Concept() { Mnemonic = "Family" },
                                Value = "Bear"
                            }
                        }
                    }
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(
                        new AssigningAuthority() { Name="OHIP", Oid="1.2.3.4.5.6", DomainName = "OHIPCARD" }, "43234453453")
                },
                Tags = new List<EntityTag>()
                {
                    new EntityTag("hasBirthCertificate", "false")
                },
                Extensions = new List<EntityExtension>() {
                    new EntityExtension()
                    {
                        ExtensionType = new ExtensionType()
                        {
                            Name = "http://santedb.org/oiz/birthcertificate",
                            ExtensionHandler = typeof(EntityPersistenceServiceTest)
                        },
                        ExtensionValueXml = BitConverter.GetBytes(true)
                    }
                },
                GenderConcept = new Concept() { Mnemonic = "Female" },
                DateOfBirth = new DateTime(1999, 07, 22),
                DateOfBirthPrecision = DatePrecision.Day
            };

            var afterInsert = base.DoTestInsert(p, s_authorization);

            var qvalue = DateTimeOffset.Now.AddDays(2);
            var result = base.DoTestQuery(o => o.CreationTime < qvalue, afterInsert.Key, s_authorization);

        }
    }
}
