using SanteDB.Core.Security.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Caching.Memory;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.ADO.Test;
using SanteDB.Persistence.MDM.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SanteDB.Core.TestFramework;

namespace SanteDB.Persistence.MDM.Test
{
    /// <summary>
    /// Tests the MDM daemon service for testing capabilities
    /// </summary>
    [TestClass]
    public class MdmAssociationTest : DataTest
    {

        /// <summary>
        /// Setup the test class
        /// </summary>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            typeof(MdmDataManagementService).Equals(null); // Trick - Force test context to load
            typeof(MemoryCacheService).Equals(null);
            TestApplicationContext.TestAssembly = typeof(MdmAssociationTest).Assembly;
            TestApplicationContext.Initialize(context.DeploymentDirectory);


        }

        /// <summary>
        /// Test - When there is a record created for a subscribed type, the system should create a MASTER reference
        /// </summary>
        [TestMethod]
        public void TestShouldCreateMasterRecord()
        {
            var patientUnderTest = new Patient()
            {
                DateOfBirth = DateTime.Parse("1983-02-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.9999"), "TC-1")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };

            Patient createdPatient = null;
            try
            {
                createdPatient = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>().Insert(patientUnderTest);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.Fail(e.Message);
            }
            Assert.IsNotNull(createdPatient);
            // The creation / matching of a master record may take some time, so we need to wait for the matcher to finish
            //Thread.Sleep(1000);

            // Now attempt to query for the record just created, it should be a synthetic MASTER record
            var masterPatient = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>().Find(o => o.Identifiers.Any(i => i.Value == "TC-1"));
            Assert.AreEqual(1, masterPatient.Count());
            Assert.AreEqual("TC-1", masterPatient.First().Identifiers.First().Value);
            Assert.AreEqual("M", masterPatient.First().Tags.First().Value);
            Assert.AreEqual("mdm.type", masterPatient.First().Tags.First().TagKey);
            Assert.AreEqual(createdPatient.Key, masterPatient.First().LoadCollection<EntityRelationship>("Relationships").First().SourceEntityKey); // Ensure master is pointed properly

            // Should redirect a retrieve request
            var masterGet = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>().Get(masterPatient.First().Key.Value, Guid.Empty);
            Assert.AreEqual(masterPatient.First().Key, masterGet.Key);
            Assert.AreEqual("TC-1", masterGet.Identifiers.First().Value);
            Assert.AreEqual("M", masterGet.Tags.First().Value);
            Assert.AreEqual("mdm.type", masterGet.Tags.First().TagKey);

        }

        /// <summary>
        /// Tests that when a duplicate record exactly matches an existing record the master is linked properly
        /// </summary>
        [TestMethod]
        public void ShouldMatchExistingMaster()
        {
            var pservice = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();

            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1983-04-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.999"), "TC-2")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = 0
            };
            var localPatient1 = pservice.Insert(patient1);

            // Wait for master
            //Thread.Sleep(1000);
            // Assert that a master record was created
            var masterPatient1 = pservice.Find(o => o.Identifiers.Any(i => i.Value == "TC-2")).FirstOrDefault();
            Assert.IsNotNull(masterPatient1);
            Assert.AreEqual("TC-2", masterPatient1.Identifiers.First().Value);
            Assert.AreEqual("M", masterPatient1.Tags.First().Value);
            Assert.AreEqual("mdm.type", masterPatient1.Tags.First().TagKey);
            Assert.AreEqual(localPatient1.Key, masterPatient1.Relationships.First().SourceEntityKey); // Ensure master is pointed properly

            // Second local patient that exactly matches
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1983-04-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST2", "TEST2", "1.2.3.4.999.5"), "TC-2B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = 0
            };
            var localPatient2 = pservice.Insert(patient2);
            //Thread.Sleep(1000);
            // Assert that master was linked
            var masterPatient2 = pservice.Find(o => o.Identifiers.Any(i => i.Value == "TC-2B")).FirstOrDefault();
            Assert.IsNotNull(masterPatient2);
            Assert.AreEqual(masterPatient1.Key, masterPatient2.Key);
            Assert.AreEqual(2, masterPatient2.Identifiers.Count()); // has both identifiers
            Assert.IsTrue(masterPatient2.Identifiers.Any(o => o.Value == "TC-2"));
            Assert.IsTrue(masterPatient2.Identifiers.Any(o => o.Value == "TC-2B"));
            Assert.AreEqual(1, masterPatient2.Names.Count());
            // Assert that both local patients are linked to MASTER
            Assert.AreEqual(2, masterPatient2.Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.MasterRecordRelationship));
            Assert.IsTrue(masterPatient2.Relationships.Any(o => o.SourceEntityKey == localPatient1.Key));
            Assert.IsTrue(masterPatient2.Relationships.Any(o => o.SourceEntityKey == localPatient2.Key));
            
        }

        /// <summary>
        /// When updating a local record with one master there should not be a new master created
        /// </summary>
        [TestMethod]
        public void UpdateShouldNotCreateNewMaster()
        {
            var pservice = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();

            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1983-05-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.999"), "TC-3")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient1 = pservice.Insert(patient1);

            // Wait for master
            //Thread.Sleep(1000);
            // Assert that a master record was created
            var masterPatientPre = pservice.Find(o => o.Identifiers.Any(i => i.Value == "TC-3")).SingleOrDefault();
            Assert.IsNotNull(masterPatientPre);
            Assert.AreEqual("TC-3", masterPatientPre.Identifiers.First().Value);
            Assert.AreEqual("M", masterPatientPre.Tags.First().Value);
            Assert.AreEqual("mdm.type", masterPatientPre.Tags.First().TagKey);
            Assert.AreEqual(localPatient1.Key, masterPatientPre.Relationships.First().SourceEntityKey); // Ensure master is pointed properly

            patient1.DateOfBirth = new DateTime(1984, 05, 22);
            patient1.Names.First().Component.First().Value = "Smithie";
            localPatient1 = pservice.Save(patient1);

            // After updating the MASTER should reflect the newest data for the local
            //Thread.Sleep(1000);
            var masterPatientPost = pservice.Find(o => o.Identifiers.Any(i => i.Value == "TC-3")).SingleOrDefault();
            Assert.AreEqual(masterPatientPre.Key, masterPatientPost.Key);
            Assert.AreEqual("Smithie", masterPatientPost.Names.First().Component.First().Value);
            Assert.AreEqual("1984-05-22", masterPatientPost.DateOfBirth.Value.ToString("yyyy-MM-dd"));
            Assert.AreEqual(1, masterPatientPost.Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.MasterRecordRelationship));
        }

        /// <summary>
        /// Test that a match with more than one master results in a new master and two probable matches
        /// </summary>
        [TestMethod]
        public void TestProbableMatch()
        {
            var pservice = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();
            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1984-01-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.999"), "TC-4A")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = 1
            };
            var localPatient1 = pservice.Insert(patient1);
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1984-01-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.999"), "TC-4B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = 2
            };
            var localPatient2 = pservice.Insert(patient2);

            // There should be two masters
            //Thread.Sleep(1000);
            var masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-4")));
            Assert.AreEqual(2, masters.Count());

            // Insert a local record that will trigger a MATCH with both local patients
            var patient3 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1984-01-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.999"), "TC-4C")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = null
            };
            var localPatient3 = pservice.Insert(patient3);
            //Thread.Sleep(1000);

            // The previous insert should result in a new MASTER being created
            masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-4")));
            Assert.AreEqual(3, masters.Count());
            var patient3Master = pservice.Find(o => o.Identifiers.Any(i => i.Value == "TC-4C")).SingleOrDefault();
            Assert.IsNotNull(patient3Master);

            // There should be 2 probables
            Assert.AreEqual(2, patient3Master.Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.CandidateLocalRelationship));
            Assert.AreEqual(2, patient3Master.Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.CandidateLocalRelationship && masters.Any(m => m.Key == r.TargetEntityKey)));

        }

        /// <summary>
        /// Tests that a non-matching local is updated to meet matching criteria. The MDM should mark the new master because it is automerge
        /// </summary>
        [TestMethod]
        public void TestNonMatchUpdatedToMatch()
        {
            var pservice = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();
            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-01-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.5.5"), "TC-5A")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient1 = pservice.Insert(patient1);
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-01-05"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.5.6"), "TC-5B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient2 = pservice.Insert(patient2);

            // There should be two masters
            //Thread.Sleep(1000);
            var masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-5")));
            Assert.AreEqual(2, masters.Count());

            // Now update patient 2 
            patient2.DateOfBirth = new DateTime(1985, 01, 04);
            localPatient2 = pservice.Save(patient2);
            //Thread.Sleep(1000);
            masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-5")));
            // There should now be one master
            Assert.AreEqual(1, masters.Count());
            Assert.AreEqual(2, masters.First().Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.MasterRecordRelationship));
            Assert.AreEqual(2, masters.First().Identifiers.Count());
            Assert.AreEqual(1, masters.First().Names.Count());
        }

        /// <summary>
        /// Test that when a matching record is updated to be a non-match that the non-matching local record is detached from the master
        /// </summary>
        [TestMethod]
        public void TestMatchUpdatedToNonMatch()
        {
            var pservice = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();
            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-06-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.5.5"), "TC-6A")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient1 = pservice.Insert(patient1);
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-06-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.5.6"), "TC-6B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient2 = pservice.Insert(patient2);

            // There should be one masters
            //Thread.Sleep(1000);
            var masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-6")));
            Assert.AreEqual(1, masters.Count());

            // Now we want to update the second local patient
            // The expected behavior is that there will be two masters with the second detached
            patient2.DateOfBirth = new DateTime(1985, 06, 06);
            localPatient2 = pservice.Save(patient2);
            //Thread.Sleep(1000);
            masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-6")));
            Assert.AreEqual(2, masters.Count());

        }

        /// <summary>
        /// Tests that when two local records are matched to a single master and a local is updated, 
        /// that the master remains the only master and the information is reflected
        /// </summary>
        [TestMethod]
        public void UpdateToMatchShouldReflectWithNoNewMaster()
        {
            var pservice = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();
            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-07-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.5.5"), "TC-7A")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient1 = pservice.Insert(patient1);
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-07-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST7", "TEST7", "1.2.3.4.5.7"), "TC-7B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient2 = pservice.Insert(patient2);

            // Thread.Sleep(1000);
            var masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-7")));
            Assert.AreEqual(1, masters.Count());
            Assert.AreEqual(1, masters.First().Names.Count);
            Assert.AreEqual(0, masters.First().Addresses.Count);

            // Update patient 2 to have a new name and an address
            patient2.Names.Clear();
            patient2.Names.Add(new EntityName(NameUseKeys.OfficialRecord, "SMITH", "JOHN"));
            patient2.Addresses.Add(new EntityAddress(AddressUseKeys.HomeAddress, "123 Main Street West", "Hamilton", "ON", "CA", "L8K5N2"));
            localPatient2 = pservice.Save(patient2);

            // Thread.Sleep(1000);
            masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-7")));
            Assert.AreEqual(1, masters.Count());
            Assert.AreEqual(2, masters.First().Names.Count);
            Assert.IsTrue(masters.First().Names.Any(n => n.Component.Any(c => c.Value == "SMITH")));
            Assert.AreEqual(1, masters.First().Addresses.Count);
            Assert.AreEqual(2, masters.First().Identifiers.Count);
            Assert.AreEqual("Male", masters.First().LoadProperty<Concept>("GenderConcept").Mnemonic);
        }

        /// <summary>
        /// Tests that when a master record contains taboo information, it is not disclosed on query.
        /// </summary>
        [TestMethod]
        public void TestTabooInformationNotDisclosedInMaster()
        {
            var pservice = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();
            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-07-06"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.5.5"), "TC-8A")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient1 = pservice.Insert(patient1);

            // Here patient 2 has some unique identifier information for an HIV clinic including his HIV alias name
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-07-06"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Anonymous, "John Brenner")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("HIVCLINIC", "HIVCLINIC", "9.9.9.9.9"), "TC-8B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            patient2.AddPolicy(DataPolicyIdentifiers.RestrictedInformation);
            var localPatient2 = pservice.Insert(patient2);

            // Thread.Sleep(1000);
            var masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-8")));
            Assert.AreEqual(1, masters.Count());
            // Note that only one identifier from the locals can be in the master synthetic
            Assert.AreEqual(1, masters.First().Names.Count);
            Assert.AreEqual(1, masters.First().Identifiers.Count);
            Assert.IsFalse(masters.First().Identifiers.Any(o => o.Value == "TC-8B")); // Shoudl not contain the HIV identifier
        }

        /// <summary>
        /// Tests that a master record with Taboo information properly almagamates information from the locals when the current user principal is elevated properly
        /// </summary>
        [TestMethod]
        public void TestShouldShowTabooInformationForAppropriateUser()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            // Create a user and authenticate as that user which has access to taboo information
            var roleService = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            roleService.CreateRole("RESTRICTED_USERS", AuthenticationContext.SystemPrincipal);
            var userService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            userService.CreateIdentity("RESTRICTED",  "TEST123", AuthenticationContext.SystemPrincipal);
            roleService.AddUsersToRoles(new string[] { "RESTRICTED" }, new string[] { "RESTRICTED_USERS",  "CLINICAL_STAFF" }, AuthenticationContext.SystemPrincipal);
            // Add security policy to the newly created role
            var role = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().GetRole("RESTRICTED_USERS");
            ApplicationServiceContext.Current.GetService<IPolicyInformationService>().AddPolicies(role, PolicyGrantType.Grant, AuthenticationContext.SystemPrincipal, DataPolicyIdentifiers.RestrictedInformation);

             // Now we're going insert two patients, one with HIV data
            var pservice = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();
            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1990-07-06"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.5.5"), "TC-9A")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient1 = pservice.Insert(patient1);

            // Here patient 2 has some unique identifier information for an HIV clinic including his HIV alias name
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1990-07-06"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Anonymous, "John Brenner")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("HIVCLINIC", "HIVCLINIC", "9.9.9.9.9"), "TC-9B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            patient2.AddPolicy(DataPolicyIdentifiers.RestrictedInformation);
            var localPatient2 = pservice.Insert(patient2);

            // Thread.Sleep(1000);
            // When running as SYSTEM - A user which does not have access
            var masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-9")));
            Assert.AreEqual(1, masters.Count());
            // Note that only one identifier from the locals can be in the master synthetic
            Assert.AreEqual(1, masters.First().Names.Count);
            Assert.AreEqual(1, masters.First().Identifiers.Count);
            Assert.IsFalse(masters.First().Identifiers.Any(o => o.Value == "TC-9B")); // Shoudl not contain the HIV identifier
            Assert.AreEqual(1, masters.First().Policies.Count);  // Should not contains any policies

            // When running as our user which has access
            var restrictedUser = userService.Authenticate("RESTRICTED", "TEST123");
            AuthenticationContext.Current = new AuthenticationContext(restrictedUser);
            masters = pservice.Find(o => o.Identifiers.Any(i => i.Value.Contains("TC-9")));
            Assert.AreEqual(1, masters.Count());
            // Note that two identifiers from the locals should be in the synthetic
            Assert.AreEqual(2, masters.First().Names.Count);
            Assert.AreEqual(2, masters.First().Identifiers.Count);
            Assert.IsTrue(masters.First().Identifiers.Any(o => o.Value == "TC-9B")); // Shoudl not contain the HIV identifier
            Assert.AreEqual(1, masters.First().Policies.Count);

        }

        /// <summary>
        /// Test - When submitting patients in a bundle each of the individual MDM rules should run
        /// </summary>
        [TestMethod]
        public void TestShouldProcessBundle()
        {
            var patientUnderTest = new Patient()
            {
                DateOfBirth = DateTime.Parse("1983-02-20"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST-MDM", "TEST-MDM", "1.2.3.4.999"), "TC-10")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };

            try
            {
                ApplicationServiceContext.Current.GetService<IRepositoryService<Bundle>>().Insert(new Bundle()
                {
                    Item = { patientUnderTest }
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.Fail(e.Message);
            }
            // The creation / matching of a master record may take some time, so we need to wait for the matcher to finish
            //Thread.Sleep(1000);

            // Now attempt to query for the record just created, it should be a synthetic MASTER record
            var masterPatient = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>().Find(o => o.Identifiers.Any(i => i.Value == "TC-10"));
            Assert.AreEqual(1, masterPatient.Count());
            Assert.AreEqual("TC-10", masterPatient.First().Identifiers.First().Value);
            Assert.AreEqual("M", masterPatient.First().Tags.First().Value);
            Assert.AreEqual("mdm.type", masterPatient.First().Tags.First().TagKey);
            Assert.AreEqual(patientUnderTest.Key, masterPatient.First().Relationships.First().SourceEntityKey); // Ensure master is pointed properly

            // Should redirect a retrieve request
            var masterGet = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>().Get(masterPatient.First().Key.Value, Guid.Empty);
            Assert.AreEqual(masterPatient.First().Key, masterGet.Key);
            Assert.AreEqual("TC-10", masterGet.Identifiers.First().Value);
            Assert.AreEqual("M", masterGet.Tags.First().Value);
            Assert.AreEqual("mdm.type", masterGet.Tags.First().TagKey);

        }
    }
}
