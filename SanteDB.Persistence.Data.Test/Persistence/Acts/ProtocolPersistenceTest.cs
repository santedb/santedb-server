/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-9-7
 */
using SanteDB.Core.Model;
using NUnit.Framework;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Protocol;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Exceptions;
using SanteDB.Core;
using SanteDB.Core.Services;

namespace SanteDB.Persistence.Data.Test.Persistence.Acts
{
    /// <summary>
    /// A test for protocol persistence
    /// </summary>
    [TestFixture()]
    [ExcludeFromCodeCoverage]
    public class ProtocolPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Protocol handler class
        /// </summary>
        [ExcludeFromCodeCoverage]
        private class DummyProtocolHandler : IClinicalProtocol
        {

            // Protocol
            private Protocol m_protocol;

            public Guid Id => this.m_protocol.Key.Value;

            public string Name => this.m_protocol.Name;

            public string Version => "1.0";

            public IEnumerable<Act> Calculate(Patient p, IDictionary<string, object> parameters)
            {
                yield break;
            }

            public DummyProtocolHandler()
            {

            }

            public DummyProtocolHandler(Protocol protocol)
            {
                this.m_protocol = protocol;
            }

            public Protocol GetProtocolData()
            {
                return this.m_protocol;
            }

            public IClinicalProtocol Load(Protocol protocolData)
            {
                this.m_protocol = protocolData;
                return this;
            }

            public void Prepare(Patient p, IDictionary<string, object> parameters)
            {
                ;
            }

            public IEnumerable<Act> Update(Patient p, IEnumerable<Act> existingPlan)
            {
                yield break;
            }
        }

        /// <summary>
        /// Test that new protocols can be persisted, and then loaded back from the protocol persistence service
        /// </summary>
        [Test]
        public void TestPersistProtocol()
        {
            // First we create a protocol
            var protocol = new Protocol()
            {
                Definition = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                HandlerClass = typeof(DummyProtocolHandler),
                Oid = "1.2.3.4.5.6",
                Name = "Teapot Protocol",
                Narrative = Narrative.DocumentFromString("Teapot" ,"en", "text/plain", "I'm a little teapot!")
            };

            using(AuthenticationContext.EnterSystemContext())
            {

                // Insert the protocol
                var afterInsert = base.TestInsert(protocol);
                Assert.AreEqual(AuthenticationContext.SystemUserSid, afterInsert.CreatedByKey.Value.ToString());
                Assert.AreEqual(10, afterInsert.Definition.Length);
                Assert.AreEqual("1.2.3.4.5.6", afterInsert.Oid);
                Assert.IsNull(afterInsert.Narrative);
                Assert.IsNotNull(afterInsert.LoadProperty(o => o.Narrative));
                Assert.AreEqual("I'm a little teapot!", Encoding.UTF8.GetString(afterInsert.Narrative.Text));

                // Test querying the protocol
                var afterQuery = base.TestQuery<Protocol>(o => o.Oid == "1.2.3.4.5.6", 1).AsResultSet().First();
                base.TestQuery<Protocol>(o => o.Oid == "1.2.3.4.5.76", 0);
                base.TestQuery<Protocol>(o => o.ObsoletionTime == null && o.Name == "Teapot Protocol", 1);
                Assert.AreEqual(AuthenticationContext.SystemUserSid, afterInsert.CreatedByKey.Value.ToString());
                Assert.AreEqual(10, afterQuery.Definition.Length);
                Assert.AreEqual("1.2.3.4.5.6", afterQuery.Oid);
                Assert.IsNull(afterQuery.Narrative);
                Assert.IsNotNull(afterQuery.LoadProperty(o => o.Narrative));
                Assert.AreEqual("I'm a little teapot!", Encoding.UTF8.GetString(afterQuery.Narrative.Text));

                // Test the update of a protocol
                var afterUpdate = base.TestUpdate(afterQuery, o =>
                {
                    o.Narrative.SetText("I am not a little teapot!");
                    o.Name = "Non-Teapot Protocol";
                    o.Oid = "6.5.4.3.2.1";
                    o.Definition = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                    return o;
                });
                Assert.AreEqual(20, afterUpdate.Definition.Length);
                Assert.AreEqual("Non-Teapot Protocol", afterUpdate.Name);

                // Validate query
                base.TestQuery<Protocol>(o => o.Oid == "1.2.3.4.5.6", 0);
                base.TestQuery<Protocol>(o => o.Oid == "6.5.4.3.2.1", 1);

                // Delete 
                base.TestDelete(afterUpdate, Core.Services.DeleteMode.LogicalDelete);
                // Validate delete
                base.TestQuery<Protocol>(o => o.Oid == "6.5.4.3.2.1", 0);
                base.TestQuery<Protocol>(o => o.Oid == "6.5.4.3.2.1" && o.ObsoletionTime != null, 1);

                // Un-delete
                base.TestUpdate(afterQuery, o => o);
                // Validate un-delete
                base.TestQuery<Protocol>(o => o.Oid == "6.5.4.3.2.1", 1);
                base.TestQuery<Protocol>(o => o.Oid == "6.5.4.3.2.1" && o.ObsoletionTime != null, 0);

                // Perma delete
                base.TestDelete(afterUpdate, Core.Services.DeleteMode.PermanentDelete);
                // Validate delete
                base.TestQuery<Protocol>(o => o.Oid == "6.5.4.3.2.1", 0);
                base.TestQuery<Protocol>(o => o.Oid == "6.5.4.3.2.1" && o.ObsoletionTime != null, 0);

                // Validate non-un-delete
                try
                {
                    // Un-delete
                    base.TestUpdate(afterQuery, o => o);
                    Assert.Fail("Should have thrown exception");
                }
                catch(DataPersistenceException e) when (e.InnerException is KeyNotFoundException)
                {

                }
                catch
                {
                    Assert.Fail("Wrong type of exception thrown!");
                }

            }
        }

        /// <summary>
        /// Test the clinical protocol repository functions work for converting data to/from database to the CDSS execution environment
        /// </summary>
        [Test]
        public void TestClinicalProtocolRepository()
        {
            // First we create a protocol
            var protocol = new Protocol()
            {
                Definition = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                HandlerClass = typeof(DummyProtocolHandler),
                Oid = "1.2.3.4.5.7",
                Name = "Teapot Protocol 2",
                Narrative = Narrative.DocumentFromString("Teapot", "en", "text/plain", "I'm a little teapot!")
            };

            using (AuthenticationContext.EnterSystemContext())
            {

                // We want to create the IClinicalProtocol instance
                var tde = new DummyProtocolHandler(protocol);
                var service = ApplicationServiceContext.Current.GetService<IClinicalProtocolRepositoryService>();
                Assert.IsNotNull(service);

                var afterInsert = service.InsertProtocol(tde);
                Assert.AreEqual(tde.Name, afterInsert.Name);

                // Now attempt to load
                var afterGet = service.GetProtocol(afterInsert.Id);
                Assert.AreEqual(tde.Name, afterGet.Name);

                // Attempt to search
                var afterSearch = service.FindProtocol(protocolName: "Teapot Protocol 2");
                Assert.AreEqual(1, afterSearch.Count());
                Assert.AreEqual(tde.Name, afterSearch.First().Name);
            }
        }
    }
}
