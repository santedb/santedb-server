using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;

namespace SanteDB.Persistence.Data.ADO.Test
{

    /// <summary>
    /// 
    /// </summary>
    [TestClass]
    [DeploymentItem(@"santedb_archive.fdb")]
    public class ArchiveServiceTest : DataTest
    {
        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            TestApplicationContext.TestAssembly = typeof(AdoIdentityProviderTest).Assembly;
            TestApplicationContext.Initialize(context.DeploymentDirectory);
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
        }

        [TestMethod]
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

        [TestMethod]
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
