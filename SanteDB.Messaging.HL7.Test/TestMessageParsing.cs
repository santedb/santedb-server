using System;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core.Security;
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

        }
    }
}
