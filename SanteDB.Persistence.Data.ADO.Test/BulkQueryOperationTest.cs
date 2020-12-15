using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using SanteDB.Core.Model;
using System.Linq;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;

namespace SanteDB.Persistence.Data.ADO.Test
{
    [TestClass]
    public class BulkQueryOperationTest :  DataTest
    {
        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            TestApplicationContext.TestAssembly = typeof(AdoIdentityProviderTest).Assembly;
            TestApplicationContext.Initialize(context.DeploymentDirectory);
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
        }

        /// <summary>
        /// This method ensures that the QueryKeys() function returns appropriate values
        /// </summary>
        [TestMethod]
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
        [TestMethod]
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
        [TestMethod]
        public void TestShouldPurgeData()
        {
            var bulkService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Place>>() as IBulkDataPersistenceService;
            Assert.IsNotNull(bulkService, "Persistence service is not IBulkDataPersistenceService");

            // Query for keys 
            var ts = new TimeSpan(1, 0, 0, 0); // 1 day
            Expression<Func<Place, bool>> expression = o => o.CreationTime.DateTime.Age(DateTime.Now) > ts;
            var keys = bulkService.QueryKeys(expression, 0, 100, out int tr);

        }
    }
}
