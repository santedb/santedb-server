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
            return MessageUtils.EncodeMessage(msg, "2.5");
        }
    }
}
