using NUnit.Framework;
using SanteDB.Core.Security;
using SanteDB.Core.TestFramework;
using SanteDB.Persistence.Data.ADO.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Tests
{
    [SetUpFixture]
    public class FixtureInitialize
    {
        [OneTimeSetUp]
        public void ClassSetup()
        {
            TestApplicationContext.TestAssembly = typeof(AdoIdentityProviderTest).Assembly;
            TestApplicationContext.Initialize(TestContext.CurrentContext.TestDirectory);

            AuthenticationContext.EnterSystemContext();

        }
    }
}
