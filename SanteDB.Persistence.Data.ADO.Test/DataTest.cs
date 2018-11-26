using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Persistence.Data.ADO.Services;
using System;
using System.IO;

namespace SanteDB.Persistence.Data.ADO.Test
{
    /// <summary>
    /// Represents an abstract data test tool
    /// </summary>
    [DeploymentItem(@"santedb_test.fdb")]
    [DeploymentItem(@"fbclient.dll")]
    [DeploymentItem(@"firebird.conf")]
    [DeploymentItem(@"firebird.msg")]
    [DeploymentItem(@"ib_util.dll")]
    [DeploymentItem(@"icudt52.dll")]
    [DeploymentItem(@"icudt52l.dat")]
    [DeploymentItem(@"icuin52.dll")]
    [DeploymentItem(@"icuuc52.dll")]
    [DeploymentItem(@"plugins\engine12.dll", "plugins")]
    public abstract class DataTest
    {

        public static class DataTestUtil
        {
            static bool started = false;

            /// <summary>
            /// Start the test context
            /// </summary>
            public static void Start(TestContext context)
            {

                if (started) return;

                AppDomain.CurrentDomain.SetData(
                   "DataDirectory",
                   Path.Combine(context.TestDeploymentDir, string.Empty));

                EntitySource.Current = new EntitySource(new PersistenceServiceEntitySource());
                ApplicationContext.Current.Start();
                var f = typeof(FirebirdSql.Data.FirebirdClient.FirebirdClientFactory).AssemblyQualifiedName;

                // Start the daemon services
                var adoPersistenceService = ApplicationServiceContext.Current.GetService<AdoPersistenceService>();
                if (!adoPersistenceService.IsRunning)
                {
                    //adoPersistenceService.Start();
                    ApplicationContext.Current.Start();
                }
                started = true;
            }
        }

        /// <summary>
        /// Starts the data test 
        /// </summary>
        public DataTest()
        {
        }
    }
}