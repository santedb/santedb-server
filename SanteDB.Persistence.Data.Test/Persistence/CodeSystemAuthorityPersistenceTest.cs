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
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test.Persistence
{
    /// <summary>
    /// Tests the assigning authority persistence service
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class CodeSystemPersistenceTest : DataPersistenceTest
    {
        /// <summary>
        /// Tests that a simple assigning authority can be inserted
        /// and then retrieved
        /// </summary>
        [Test]
        public void TestInsertCodeSystem()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<CodeSystem>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var cs = persistenceService.Insert(new CodeSystem()
            {
                Description = "A test code system",
                Name = "TEST_CS_01",
                Oid = "1.2.3",
                Url = "http://google.com/test1"
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(cs.Key);
            Assert.IsNotNull(cs.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, cs.CreatedByKey.ToString());
            Assert.IsNotNull(cs.CreationTime);
            Assert.AreEqual("TEST_CS_01", cs.Name);

            // Now, re-fetch and validate
            var cs2 = persistenceService.Get(cs.Key.Value, null, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(cs2);
            Assert.AreEqual(cs.Key, cs2.Key);
            Assert.AreEqual(cs.Oid, cs2.Oid);
            Assert.AreEqual(cs.Url, cs2.Url);

            // Now fetch by name
            var cs3 = persistenceService.Query(o => o.Url == "http://google.com/test1", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, cs3.Count());
            Assert.AreEqual(cs.Key, cs3.First().Key);
        }

        /// <summary>
        /// Test that the update of an assigning authority is successful
        /// </summary>
        [Test]
        public void TestUpdateCodeSystem()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<CodeSystem>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var cs = persistenceService.Insert(new CodeSystem()
            {
                Description = "A test system",
                Name = "TEST_CS_03",
                Oid = "1.2.3.3",
                Url = "http://google.com/test3"
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(cs.Key);
            Assert.IsNotNull(cs.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, cs.CreatedByKey.ToString());
            Assert.IsNotNull(cs.CreationTime);

            // Next update
            cs.Oid = "3.3.2.1";
            var cs2 = persistenceService.Update(cs, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            Assert.AreEqual("3.3.2.1", cs2.Oid);
        }

        /// <summary>
        /// Tests that obsoletiong of a simple assigning authority removal
        /// </summary>
        [Test]
        public void TestObsoleteCodeSystem()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<CodeSystem>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var cs = persistenceService.Insert(new CodeSystem()
            {
                Description = "A test authority",
                Name = "TEST_CS_05",
                Oid = "1.2.3.5",
                Url = "http://google.com/test5"
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(cs.Key);
            Assert.IsNotNull(cs.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, cs.CreatedByKey.ToString());
            Assert.IsNotNull(cs.CreationTime);

            var cs2 = persistenceService.Delete(cs.Key.Value, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(cs2.ObsoletionTime);
            Assert.IsNotNull(cs2.ObsoletedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, cs2.ObsoletedByKey.ToString());

            // Validate that the CS is retrievable by fetch
            var cs3 = persistenceService.Get(cs.Key.Value, null, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(cs3);

            // Validate that CS is not found via the query method
            var cs4 = persistenceService.Query(o => o.Url == "http://google.com/test5", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(0, cs4.Count());

            // Validate that AA can be found when explicitly querying for obsoleted
            var cs5 = persistenceService.Query(o => o.Url == "http://google.com/test5" && o.ObsoletionTime != null, AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, cs5.Count());
        }

        /// <summary>
        /// Tests the un-deletion of an AA
        /// </summary>
        [Test]
        public void TestUnDeleteCodeSystem()
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<CodeSystem>>();
            Assert.IsNotNull(persistenceService);

            // Insert an assigning authority
            var cs = persistenceService.Insert(new CodeSystem()
            {
                Description = "A test authority",
                Name = "TEST_AA_07",
                Oid = "1.2.3.7",
                Url = "http://google.com/test7"
            }, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);

            // First, assert that proper insert columns are set
            Assert.IsNotNull(cs.Key);
            Assert.IsNotNull(cs.CreatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, cs.CreatedByKey.ToString());
            Assert.IsNotNull(cs.CreationTime);

            var cs2 = persistenceService.Delete(cs.Key.Value, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(cs2.ObsoletionTime);
            Assert.IsNotNull(cs2.ObsoletedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, cs2.ObsoletedByKey.ToString());

            // Validate that CS is not found via the query method
            var cs3 = persistenceService.Query(o => o.Url == "http://google.com/test7", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(0, cs3.Count());

            var cs4 = persistenceService.Update(cs2, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
            Assert.IsNull(cs4.ObsoletedByKey);
            Assert.IsNull(cs4.ObsoletionTime);
            Assert.IsNotNull(cs4.UpdatedByKey);
            Assert.AreEqual(AuthenticationContext.SystemUserSid, cs4.UpdatedByKey.ToString());

            // Validate that AA is now found via the query method
            var cs5 = persistenceService.Query(o => o.Url == "http://google.com/test7", AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, cs5.Count());
        }
    }
}