using NUnit.Framework;
using SanteDB.Core.TestFramework;
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

        }
    }
}
