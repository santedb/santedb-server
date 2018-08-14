using System;
using MARC.HI.EHRS.SVC.Messaging.HAPI.TransportProtocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Messaging.HL7.Messages;

namespace SanteDB.Messaging.HL7.Test
{
    [TestClass]
    public class TestMessageParsing
    {
        /// <summary>
        /// Test that ADT message is parsed properly
        /// </summary>
        [TestMethod]
        public void TestParseADTMessage()
        {
            var msg = TestUtil.GetMessage("HL7ADT1");
            var message = new AdtMessageHandler().HandleMessage(new Hl7MessageReceivedEventArgs(msg, new Uri("test://"), new Uri("test://"), DateTime.Now));

        }
    }
}
