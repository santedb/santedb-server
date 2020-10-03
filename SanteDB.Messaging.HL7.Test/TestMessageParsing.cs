using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHapi.Base.Model;
using NHapi.Model.V25.Segment;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.HL7.Messages;
using SanteDB.Messaging.HL7.TransportProtocol;
using SanteDB.Core.TestFramework;
using SanteDB.Persistence.Data.ADO.Test;
using System;
using System.Linq;
using NHapi.Model.V25.Message;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Entities;

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
            // Force load of the DLL
            var p = FirebirdSql.Data.FirebirdClient.FbCharset.Ascii;
            TestApplicationContext.TestAssembly = typeof(TestMessageParsing).Assembly;
            TestApplicationContext.Initialize(context.DeploymentDirectory);

            // Create the test harness device / application
            var securityDevService = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityDevice>>();
            var securityAppService = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityApplication>>();
            var metadataService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
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
                IsUnique = true,
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
            var patient = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-1"), AuthenticationContext.Current.Principal).SingleOrDefault();
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
        public void TestUpdateAdt()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var msg = TestUtil.GetMessage("ADT_SIMPLE");
            var message = new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(message);
            Assert.AreEqual("CA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);

            var patientOriginal = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-1"), AuthenticationContext.Current.Principal).SingleOrDefault();

            msg = TestUtil.GetMessage("ADT_UPDATE");
            message = new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            messageStr = TestUtil.ToString(message);
            Assert.AreEqual("CA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);

            // Ensure that the patient actually was persisted
            var patientNew = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-1"), AuthenticationContext.Current.Principal).SingleOrDefault();
            Assert.IsNotNull(patientNew);
            Assert.IsTrue(messageStr.Contains(patientNew.Key.ToString()));
            Assert.AreEqual(1, patientNew.Names.Count);
            Assert.AreEqual("JOHNSTON", patientNew.Names.First().Component.First(o => o.ComponentTypeKey == NameComponentKeys.Family).Value);
            Assert.AreEqual("ROBERTA", patientNew.Names.First().Component.First(o => o.ComponentTypeKey == NameComponentKeys.Given).Value);
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
            var patient = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-2"), AuthenticationContext.Current.Principal).SingleOrDefault();
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
            var patient = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-3"), AuthenticationContext.Current.Principal).SingleOrDefault();
            Assert.IsNotNull(patient);
            msg = TestUtil.GetMessage("QBP_SIMPLE");
            var message = new QbpMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(message);
            Assert.AreEqual("SMITH", ((message.GetStructure("QUERY_RESPONSE") as AbstractGroup).GetStructure("PID") as PID).GetMotherSMaidenName(0).FamilyName.Surname.Value);
            Assert.AreEqual("AA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);
            Assert.AreEqual("OK", (message.GetStructure("QAK") as QAK).QueryResponseStatus.Value);
            Assert.AreEqual("K22", (message.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value);
        }

        /// <summary>
        /// Tests that a query actually occurs
        /// </summary>
        [TestMethod]
        public void TestParseComplexQBPMessage()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var msg = TestUtil.GetMessage("QBP_COMPLEX_PRE");
            new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var patient = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-9"), AuthenticationContext.Current.Principal).SingleOrDefault();
            Assert.IsNotNull(patient);
            Assert.AreEqual(6, patient.LoadCollection<EntityRelationship>(nameof(Entity.Relationships)).Count());
            Assert.IsNotNull(patient.LoadCollection<EntityRelationship>(nameof(Entity.Relationships)).FirstOrDefault(o=>o.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother));
            msg = TestUtil.GetMessage("QBP_COMPLEX");
            var message = new QbpMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(message);
            Assert.AreEqual("SMITH", ((message.GetStructure("QUERY_RESPONSE") as AbstractGroup).GetStructure("PID") as PID).GetMotherSMaidenName(0).FamilyName.Surname.Value, $"Mothers name doesn't match {messageStr}");
            Assert.AreEqual("AA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);
            Assert.AreEqual("OK", (message.GetStructure("QAK") as QAK).QueryResponseStatus.Value);
            Assert.AreEqual("K22", (message.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value);
        }


        /// <summary>
        /// Tests that a query actually occurs
        /// </summary>
        [TestMethod]
        public void TestParseAndQBPMessage()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var msg = TestUtil.GetMessage("QBP_COMPLEX_PRE");
            var response = new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var patient = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-9"), AuthenticationContext.Current.Principal).SingleOrDefault();
            Assert.IsNotNull(patient);
            msg = TestUtil.GetMessage("QBP_AND_PRE");
            new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            patient = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-10"), AuthenticationContext.Current.Principal).SingleOrDefault();
            Assert.IsNotNull(patient);

            msg = TestUtil.GetMessage("QBP_COMPLEX");
            var message = new QbpMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(message);
            Assert.AreEqual("1", (message.GetStructure("QAK") as QAK).HitCount.Value);
            Assert.AreEqual("SMITH", ((message.GetStructure("QUERY_RESPONSE") as AbstractGroup).GetStructure("PID") as PID).GetMotherSMaidenName(0).FamilyName.Surname.Value);
            Assert.AreNotEqual("JENNY", ((message.GetStructure("QUERY_RESPONSE") as AbstractGroup).GetStructure("PID") as PID).GetPatientName(0).GivenName.Value);
            Assert.AreEqual("AA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);
            Assert.AreEqual("OK", (message.GetStructure("QAK") as QAK).QueryResponseStatus.Value);
            Assert.AreEqual("K22", (message.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value);

            // OR MESSAGE SHOULD CATCH TWO PATIENTS
            msg = TestUtil.GetMessage("QBP_OR");
            message = new QbpMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            messageStr = TestUtil.ToString(message);
            Assert.AreEqual("2", (message.GetStructure("QAK") as QAK).HitCount.Value);
            Assert.AreEqual("AA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);
            Assert.AreEqual("OK", (message.GetStructure("QAK") as QAK).QueryResponseStatus.Value);
            Assert.AreEqual("K22", (message.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value);
        }
        /// <summary>
        /// Tests that the error code and location are appropriate for the type of error that is encountered
        /// </summary>
        [TestMethod]
        public void TestErrorLocation()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var msg = TestUtil.GetMessage("ADT_INV_GC");
            var errmsg = new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(errmsg);

            var ack = errmsg as ACK;
            Assert.AreNotEqual(0, ack.ERRRepetitionsUsed);
            Assert.AreEqual("204", ack.GetERR(0).HL7ErrorCode.Identifier.Value);
            Assert.AreEqual("8", ack.GetERR(0).GetErrorLocation(0).FieldPosition.Value);
            Assert.AreEqual("PID", ack.GetERR(0).GetErrorLocation(0).SegmentID.Value);
            Assert.AreEqual("1", ack.GetERR(0).GetErrorLocation(0).SegmentSequence.Value);

        }

        /// <summary>
        /// Tests that a query actually occurs
        /// </summary>
        [TestMethod]
        public void TestCrossReference()
        {
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var msg = TestUtil.GetMessage("QBP_XREF_PRE");
            new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var patient = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>().Query(o => o.Identifiers.Any(i => i.Value == "HL7-4"), AuthenticationContext.Current.Principal).SingleOrDefault();
            Assert.IsNotNull(patient);
            msg = TestUtil.GetMessage("QBP_XREF");
            var message = new QbpMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));
            var messageStr = TestUtil.ToString(message);
            // TODO : Assert that id is present
            Assert.IsTrue(((message.GetStructure("QUERY_RESPONSE") as AbstractGroup).GetStructure("PID") as PID).GetPatientIdentifierList().Any(i => i.IDNumber.Value == patient.Key.ToString() && i.AssigningAuthority.NamespaceID.Value == "KEY"));
            Assert.AreEqual("AA", (message.GetStructure("MSA") as MSA).AcknowledgmentCode.Value);
            Assert.AreEqual("OK", (message.GetStructure("QAK") as QAK).QueryResponseStatus.Value);
            Assert.AreEqual("K23", (message.GetStructure("MSH") as MSH).MessageType.TriggerEvent.Value);
        }
    }
}
