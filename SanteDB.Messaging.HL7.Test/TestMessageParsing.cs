using System;
using System.Linq;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHapi.Model.V25.Segment;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Messages;
using SanteDB.Persistence.Data.ADO.Test;

namespace SanteDB.Messaging.HL7.Test
{
    [TestClass]
    public class TestMessageParsing : DataTest
    {

        /// <summary>
        /// Test context
        /// </summary>
        /// <param name="context"></param>
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            DataTest.DataTestUtil.Start(context);

            // Create the test harness device / application
            var securityDevService = ApplicationContext.Current.GetService<IRepositoryService<SecurityDevice>>();
            var securityAppService = ApplicationContext.Current.GetService<IRepositoryService<SecurityApplication>>();
            var metadataService = ApplicationContext.Current.GetService<IAssigningAuthorityRepositoryService>();
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            // Create device
            var dev = new SecurityDevice()
            {
                DeviceSecret = "DEVICESECRET",
                Name = "TEST_HARNESS|TEST"
            };
            dev.AddPolicy(PermissionPolicyIdentifiers.LoginAsService);
            dev = securityDevService.Insert(dev);

            var app = new SecurityApplication()
            {
                Name = "TEST_HARNESS",
                ApplicationSecret = "APPLICATIONSECRET"
            };
            app.AddPolicy(PermissionPolicyIdentifiers.LoginAsService);
            app.AddPolicy(PermissionPolicyIdentifiers.UnrestrictedClinicalData);
            app.AddPolicy(PermissionPolicyIdentifiers.ReadMetadata);
            app = securityAppService.Insert(app);
            metadataService.Insert(new Core.Model.DataTypes.AssigningAuthority("TEST", "TEST", "1.2.3.4.5.6.7")
            {
                AssigningApplicationKey = app.Key
            });

            // Add another application for security checks
            dev = new SecurityDevice()
            {
                DeviceSecret = "DEVICESECRET2",
                Name = "TEST_HARNESS2|TEST"
            };
            dev.AddPolicy(PermissionPolicyIdentifiers.LoginAsService);
            dev = securityDevService.Insert(dev);

            app = new SecurityApplication()
            {
                Name = "TEST_HARNESS2",
                ApplicationSecret = "APPLICATIONSECRET2"
            };
            app.AddPolicy(PermissionPolicyIdentifiers.LoginAsService);
            app.AddPolicy(PermissionPolicyIdentifiers.UnrestrictedClinicalData);
            app.AddPolicy(PermissionPolicyIdentifiers.ReadMetadata);
            app = securityAppService.Insert(app);
        }

        /// <summary>
        /// Test that ADT message is parsed properly
        /// </summary>
        [TestMethod]
        public void TestParseADTMessage()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var msg = TestUtil.GetMessage("ADT_SIMPLE");
            var message = new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(message);
            Assert.AreEqual("CA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);

            // Ensure that the patient actually was persisted
            var patient = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-1"), AuthenticationContext.SystemPrincipal).SingleOrDefault();
            Assert.IsNotNull(patient);
            Assert.IsTrue(messageStr.Contains(patient.Key.ToString()));
            Assert.AreEqual(1, patient.Names.Count);
            Assert.AreEqual("JOHNSTON", patient.Names.First().Component.First(o => o.ComponentTypeKey == NameComponentKeys.Family).Value);
            Assert.AreEqual("ROBERT", patient.Names.First().Component.First(o => o.ComponentTypeKey == NameComponentKeys.Given).Value);
        }

        /// <summary>
        /// Test that ADT message is parsed properly
        /// </summary>
        [TestMethod]
        public void TestParseComplexADTMessage()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var msg = TestUtil.GetMessage("ADT_PD1");
            var message = new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(message);
            Assert.AreEqual("CA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);

            // Ensure that the patient actually was persisted
            var patient = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-2"), AuthenticationContext.SystemPrincipal).SingleOrDefault();
            Assert.IsNotNull(patient);
            Assert.IsTrue(messageStr.Contains(patient.Key.ToString()));
            Assert.AreEqual(1, patient.Names.Count);
            Assert.AreEqual("JOHNSTON", patient.Names.First().Component.First(o => o.ComponentTypeKey == NameComponentKeys.Family).Value);
            Assert.AreEqual("ROBERT", patient.Names.First().Component.First(o => o.ComponentTypeKey == NameComponentKeys.Given).Value);
            Assert.AreEqual(1, patient.Relationships.Count(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Birthplace));
            Assert.AreEqual(1, patient.Relationships.Count(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Father));
            Assert.AreEqual(1, patient.Relationships.Count(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother));
            Assert.AreEqual(2, patient.Relationships.Count(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Citizen));
            Assert.AreEqual(1, patient.Policies.Count);
        }

        /// <summary>
        /// Tests that a query actually occurs
        /// </summary>
        [TestMethod]
        public void TestParseQBPMessage()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var msg = TestUtil.GetMessage("QBP_SIMPLE_PRE");
            new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var patient = ApplicationContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-3"), AuthenticationContext.SystemPrincipal).SingleOrDefault();
            Assert.IsNotNull(patient);
            msg = TestUtil.GetMessage("QBP_SIMPLE");
            var message = new QbpMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(message);
            Assert.AreEqual("AA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);
            Assert.AreEqual("AA", (message.GetStructure("QAK") as QAK).QueryResponseStatus.Value);
            Assert.AreEqual("K22", (message.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value);
        }

    }
}
