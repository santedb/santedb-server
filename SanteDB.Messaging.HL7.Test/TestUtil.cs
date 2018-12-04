using NHapi.Base.Model;
using NHapi.Base.Parser;
using SanteDB.Messaging.HL7.Utils;
using System;
using System.IO;

namespace SanteDB.Messaging.HL7.Test
{
    /// <summary>
    /// Test utility classes
    /// </summary>
    public static class TestUtil
    {

        /// <summary>
        /// Get the message from the test assembly
        /// </summary>
        public static IMessage GetMessage(String messageName)
        {
            string originalVersion = null;
            using (var s = typeof(TestUtil).Assembly.GetManifestResourceStream($"SanteDB.Messaging.HL7.Test.Resources.{messageName}.txt")) 
            using (var sw = new StreamReader(s))
                return MessageUtils.ParseMessage(sw.ReadToEnd(), out originalVersion);
        }

        /// <summary>
        /// Represent message as string
        /// </summary>
        public static String ToString(IMessage msg)
        {
            
            return MessageUtils.EncodeMessage(msg, "2.5.1");
        }
    }
}
