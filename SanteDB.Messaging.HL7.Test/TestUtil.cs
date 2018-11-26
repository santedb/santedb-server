using NHapi.Base.Model;
using NHapi.Base.Parser;
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
            using (var s = typeof(TestUtil).Assembly.GetManifestResourceStream($"SanteDB.Messaging.HL7.Test.Resources.{messageName}.txt"))
            using (var sw = new StreamReader(s))
                return new PipeParser().Parse(sw.ReadToEnd().Replace("2.3.1", "2.5"));
        }

        /// <summary>
        /// Represent message as string
        /// </summary>
        public static String ToString(IMessage msg)
        {
            return new PipeParser().Encode(msg);
        }
    }
}
