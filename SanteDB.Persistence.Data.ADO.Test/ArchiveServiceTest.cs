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
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;

namespace SanteDB.Persistence.Data.ADO.Tests
{

    /// <summary>
    /// 
    /// </summary>
    [TestFixture(Category = "Persistence")]
    public class ArchiveServiceTest : DataTest
    {
        [SetUp]
        public void ClassSetup()
        {
            AuthenticationContext.EnterSystemContext();
        }

        [Test]
        public void TestCanPurgeArchive()
        {

            var archiveService = ApplicationServiceContext.Current.GetService<IDataArchiveService>();
            Assert.IsNotNull(archiveService, "Missing archive service");

            // Persistence service - We want to simulate an empty archive
            // By default we just deploy the populated test database, we want to clear out the data
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Concept>>() as IBulkDataPersistenceService;
            Expression<Func<Concept, bool>> conceptExpr = o => o.ObsoletionTime == null;
            var keys = persistenceService.QueryKeys(conceptExpr, 0, 10000, out int _).ToArray();
            if(archiveService.Exists(typeof(Concept), keys[0]))
                archiveService.Purge(typeof(Concept), keys);
            var concept = archiveService.Retrieve(typeof(Concept), keys[0]) as Concept;
            Assert.AreEqual(concept.StatusConceptKey, StatusKeys.Purged);

            persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Entity>>() as IBulkDataPersistenceService;
            Expression<Func<Entity, bool>> entityExpr = o => o.StatusConceptKey == StatusKeys.Active;
            keys = persistenceService.QueryKeys(entityExpr, 0, 10000, out int _).ToArray();
            if (archiveService.Exists(typeof(Entity), keys[0]))
                archiveService.Purge(typeof(Entity), keys);
            var entity = archiveService.Retrieve(typeof(Entity), keys[0]) as Entity;
            Assert.AreEqual(concept.StatusConceptKey, StatusKeys.Purged);

        }

        [Test]
        public void TestCanArchive()
        {

            var archiveService = ApplicationServiceContext.Current.GetService<IDataArchiveService>();
            Assert.IsNotNull(archiveService, "Missing archive service");
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Concept>>() as IBulkDataPersistenceService;
            Expression<Func<Concept, bool>> conceptExpr = o => o.StatusConceptKey != StatusKeys.Obsolete;
            var keys = persistenceService.QueryKeys(conceptExpr, 0, 10000, out int _).ToArray();
            archiveService.Archive(typeof(Concept), keys); // Archive the object

            persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Entity>>() as IBulkDataPersistenceService;
            Expression<Func<Entity, bool>> entityExpr = o => o.StatusConceptKey != StatusKeys.Obsolete;
            keys = persistenceService.QueryKeys(entityExpr, 0, 10000, out int _).ToArray();
            archiveService.Archive(typeof(Entity), keys); // Archive the object
            

        }
    }
}
