using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Caching.Memory;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.ADO.Test;
using SanteDB.Persistence.MDM.Services;

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
            typeof(MdmDaemonService).Equals(null); // Trick - Force test context to load
            typeof(MemoryCacheService).Equals(null);
            DataTest.DataTestUtil.Start(context);
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
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5"), "TC-1")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };

            Patient createdPatient = null;
            try
            {
                createdPatient = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Insert(patientUnderTest, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Assert.Fail(e.Message);
            }
            Assert.IsNotNull(createdPatient);
            // The creation / matching of a master record may take some time, so we need to wait for the matcher to finish
            Thread.Sleep(1000);

            // Now attempt to query for the record just created, it should be a synthetic MASTER record
            var masterPatient = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "TC-1"), AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, masterPatient.Count());
            Assert.AreEqual("TC-1", masterPatient.First().Identifiers.First().Value);
            Assert.AreEqual("M", masterPatient.First().Tags.First().Value);
            Assert.AreEqual("mdm.type", masterPatient.First().Tags.First().TagKey);
            Assert.AreEqual(createdPatient.Key, masterPatient.First().Relationships.First().SourceEntityKey); // Ensure master is pointed properly

            // Should redirect a retrieve request
            var masterGet = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Get<Guid>(new MARC.HI.EHRS.SVC.Core.Data.Identifier<Guid>(masterPatient.First().Key.Value), AuthenticationContext.SystemPrincipal, true);
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
            var pservice = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>();

            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1983-04-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5"), "TC-2")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = 0
            };
            var localPatient1 = pservice.Insert(patient1, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);

            // Wait for master
            Thread.Sleep(1000);
            // Assert that a master record was created
            var masterPatient1 = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "TC-2"), AuthenticationContext.SystemPrincipal).FirstOrDefault();
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
                    new EntityIdentifier(new AssigningAuthority("TEST2", "TEST2", "1.2.3.4.5.2"), "TC-2B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = 0
            };
            var localPatient2 = pservice.Insert(patient2, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            Thread.Sleep(1000);
            // Assert that master was linked
            var masterPatient2 = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "TC-2B"), AuthenticationContext.SystemPrincipal).FirstOrDefault();
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
            var pservice = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>();

            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1983-05-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5"), "TC-3")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient1 = pservice.Insert(patient1, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);

            // Wait for master
            Thread.Sleep(1000);
            // Assert that a master record was created
            var masterPatientPre = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "TC-3"), AuthenticationContext.SystemPrincipal).SingleOrDefault();
            Assert.IsNotNull(masterPatientPre);
            Assert.AreEqual("TC-3", masterPatientPre.Identifiers.First().Value);
            Assert.AreEqual("M", masterPatientPre.Tags.First().Value);
            Assert.AreEqual("mdm.type", masterPatientPre.Tags.First().TagKey);
            Assert.AreEqual(localPatient1.Key, masterPatientPre.Relationships.First().SourceEntityKey); // Ensure master is pointed properly

            patient1.DateOfBirth = new DateTime(1984, 05, 22);
            patient1.Names.First().Component.First().Value = "Smithie";
            localPatient1 = pservice.Update(patient1, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);

            // After updating the MASTER should reflect the newest data for the local
            Thread.Sleep(1000);
            var masterPatientPost = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "TC-3"), AuthenticationContext.SystemPrincipal).SingleOrDefault();
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
            var pservice = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>();
            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1984-01-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5"), "TC-4A")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = 1
            };
            var localPatient1 = pservice.Insert(patient1, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1984-01-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5"), "TC-4B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = 2
            };
            var localPatient2 = pservice.Insert(patient2, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);

            // There should be two masters
            Thread.Sleep(1000);
            var masters = pservice.Query(o => o.Identifiers.Any(i => i.Value.Contains("TC-4")), AuthenticationContext.SystemPrincipal);
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
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5"), "TC-4C")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198"),
                MultipleBirthOrder = null
            };
            var localPatient3 = pservice.Insert(patient3, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            Thread.Sleep(1000);

            // The previous insert should result in a new MASTER being created
            masters = pservice.Query(o => o.Identifiers.Any(i => i.Value.Contains("TC-4")), AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(3, masters.Count());
            var patient3Master = pservice.Query(o => o.Identifiers.Any(i => i.Value == "TC-4C"), AuthenticationContext.SystemPrincipal).SingleOrDefault();
            Assert.IsNotNull(patient3Master);

            // There should be 2 probables
            Assert.AreEqual(2, patient3Master.Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship));
            Assert.AreEqual(2, patient3Master.Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship && masters.Any(m => m.Key == r.TargetEntityKey)));

        }

        /// <summary>
        /// Tests that a non-matching local is updated to meet matching criteria. The MDM should mark the new master because it is automerge
        /// </summary>
        [TestMethod]
        public void TestNonMatchUpdatedToMatch()
        {
            var pservice = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>();
            var patient1 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-01-04"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5.5"), "TC-5A")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient1 = pservice.Insert(patient1, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            var patient2 = new Patient()
            {
                DateOfBirth = DateTime.Parse("1985-01-05"),
                Names = new List<EntityName>()
                {
                    new EntityName(NameUseKeys.Legal, "Smith", "Testy")
                },
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority("TEST", "TEST", "1.2.3.4.5.6"), "TC-5B")
                },
                GenderConceptKey = Guid.Parse("F4E3A6BB-612E-46B2-9F77-FF844D971198")
            };
            var localPatient2 = pservice.Insert(patient2, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);

            // There should be two masters
            Thread.Sleep(1000);
            var masters = pservice.Query(o => o.Identifiers.Any(i => i.Value.Contains("TC-5")), AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(2, masters.Count());

            // Now update patient 2 
            patient2.DateOfBirth = new DateTime(1985, 01, 04);
            localPatient2 = pservice.Update(patient2, AuthenticationContext.SystemPrincipal, TransactionMode.Commit);
            Thread.Sleep(1000);
            masters = pservice.Query(o => o.Identifiers.Any(i => i.Value.Contains("TC-5")), AuthenticationContext.SystemPrincipal);
            // There should now be two masters with the master of TC-5B pointing to the master of TC-5A as a probable
            Assert.AreEqual(2, masters.Count());
            Patient master2 = pservice.Query(o => o.Identifiers.Any(i => i.Value.Contains("TC-5B")), AuthenticationContext.SystemPrincipal).SingleOrDefault(),
                master1 = pservice.Query(o => o.Identifiers.Any(i => i.Value.Contains("TC-5A")), AuthenticationContext.SystemPrincipal).SingleOrDefault();
            Assert.AreEqual(1, master2.Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.MasterRecordRelationship));
            Assert.AreEqual(1, master2.Relationships.Count(r => r.RelationshipTypeKey == MdmConstants.DuplicateRecordRelationship));

            // Test merge of two 
            var mergeService = ApplicationContext.Current.GetService<IRecordMergingService<Patient>>();
            Assert.IsNotNull(mergeService);

            // Merge master2 into master1
            mergeService.Merge(master1, new List<Patient>() { master2 });
            masters = pservice.Query(o => o.Identifiers.Any(i => i.Value.Contains("TC-5")), AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, masters.Count());
        }
    }
}
