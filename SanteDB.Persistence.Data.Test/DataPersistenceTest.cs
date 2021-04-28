using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Security.Services;
using SanteDB.Core.TestFramework;
using SanteDB.Persistence.Data.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// An abstract test fixture which handles the initialization of data tests
    /// </summary>
    public abstract class DataPersistenceTest : DataTest
    {
        // Application ID service
        protected IServiceManager m_serviceManager;

        /// <summary>
        /// Setup the test
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Force load of the DLL
            var p = FirebirdSql.Data.FirebirdClient.FbCharset.Ascii;
            TestApplicationContext.TestAssembly = typeof(DataPersistenceTest).Assembly;
            TestApplicationContext.Initialize(TestContext.CurrentContext.TestDirectory);
            this.m_serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
            this.m_serviceManager.AddServiceProvider(typeof(AdoApplicationIdentityProvider));
            this.m_serviceManager.AddServiceProvider(typeof(AdoDeviceIdentityProvider));
            this.m_serviceManager.AddServiceProvider(typeof(AdoIdentityProvider));
            this.m_serviceManager.AddServiceProvider(typeof(AdoSessionProvider));


        }
    }
}
