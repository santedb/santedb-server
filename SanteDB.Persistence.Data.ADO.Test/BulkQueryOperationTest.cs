/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using SanteDB.Core.Model.Query;

namespace SanteDB.Persistence.Data.ADO.Test
{
    [ExcludeFromCodeCoverage]
    [TestFixture(Category = "Persistence")]
    public class BulkQueryOperationTest :  DataTest
    {
        [SetUp]
        public void ClassSetup()
        {
            AuthenticationContext.EnterSystemContext();
        }

        /// <summary>
        /// This method ensures that the QueryKeys() function returns appropriate values
        /// </summary>
        [Test]
        public void TestShouldQueryForKeys()
        {

            var bulkService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Concept>>() as IBulkDataPersistenceService;
            Assert.IsNotNull(bulkService, "Persistence service is not IBulkDataPersistenceService");

            // Query for keys 
            var ts = new TimeSpan(1, 0, 0, 0); // 1 day
            String[] statusMnemonics = new string[] { "COMPLETE", "ACTIVE", "NEW" };
            Expression<Func<Concept, bool>> expression = o => o.CreationTime.DateTime.Age(DateTime.Now) > ts &&  statusMnemonics.Contains(o.Mnemonic);
            var keys = bulkService.QueryKeys(expression, 0, 100, out int tr);
            Assert.AreEqual(3, keys.Count());

        }

        /// <summary>
        /// Tests that the Obsolete bulk operation works as expected
        /// </summary>
        [Test]
        public void TestShouldObsoleteData()
        {
            var bulkService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Concept>>() as IBulkDataPersistenceService;
            Assert.IsNotNull(bulkService, "Persistence service is not IBulkDataPersistenceService");

            // Query for keys 
            var ts = new TimeSpan(1, 0, 0, 0); // 1 day
            String[] statusMnemonics = new string[] { "OBSOLETE", "CANCELLED" };
            Expression<Func<Concept, bool>> expression = o => o.CreationTime.DateTime.Age(DateTime.Now) > ts && statusMnemonics.Contains(o.Mnemonic) && o.StatusConcept.Mnemonic == "ACTIVE";
            var keys = bulkService.QueryKeys(expression, 0, 100, out int tr);
            Assert.AreEqual(2, keys.Count());
            bulkService.Obsolete(TransactionMode.Commit, AuthenticationContext.SystemPrincipal, keys.ToArray());
            bulkService.QueryKeys(expression, 0, 100, out tr);
            Assert.AreEqual(0, tr);

            // Ensure that objects were obsoleted
            if(bulkService is IDataPersistenceService idp)
            {
                var o1 = idp.Get(keys.First()) as Concept;
                Assert.AreEqual(StatusKeys.Obsolete, o1.StatusConceptKey);
                o1 = idp.Get(keys.Last()) as Concept;
                Assert.AreEqual(StatusKeys.Obsolete, o1.StatusConceptKey);
            }
        }

        /// <summary>
        /// Tests that the data is purged from the database
        /// </summary>
        [Test]
        public void TestShouldPurgeData()
        {
            var bulkService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>() as IBulkDataPersistenceService;
            Assert.IsNotNull(bulkService, "Persistence service is not IBulkDataPersistenceService");

            // Query for keys 
            var ts = new TimeSpan(1, 0, 0, 0); // 1 day
            Expression<Func<Place, bool>> expression = o => o.CreationTime.DateTime.Age(DateTime.Now) > ts && o.StatusConceptKey == StatusKeys.Active;
            var keys = bulkService.QueryKeys(expression, 0, 10, out int tr);
            Assert.AreEqual(255, tr);
            Assert.AreEqual(10, keys.Count());

            // Now we want to obsolete the first 10
            bulkService.Purge(TransactionMode.Commit, AuthenticationContext.SystemPrincipal, keys.ToArray());
            var keys2 = bulkService.QueryKeys(expression, 0, 10, out tr);
            Assert.AreEqual(245, tr); // 10 should be "PURGED"
            Assert.IsFalse(keys2.SequenceEqual(keys)); // 10 keys should not be the same
        }
    }
}
