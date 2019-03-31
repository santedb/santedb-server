using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.TestFramework;
using System;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Test
{
    [TestClass]
    public class AdoPolicyProviderTest : DataTest
    {

        /// <summary>
        /// Class startup
        /// </summary>
        /// <param name="context"></param>
        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            TestApplicationContext.TestAssembly = typeof(AdoIdentityProviderTest).Assembly;
            TestApplicationContext.Initialize(context.DeploymentDirectory);

        }

        /// <summary>
        /// Test that the service gets all policies
        /// </summary>
        [TestMethod]
        public void TestGetAllPolicies()
        {

            var ipoli = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();
            Assert.IsNotNull(ipoli);
            var policies = ipoli.GetPolicies();
            Assert.AreNotEqual(0, policies.Count());

            // Assert that default policies
            foreach(var fi in typeof(PermissionPolicyIdentifiers).GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
            {
                var fiv = fi.GetValue(null);
                Assert.IsTrue(policies.Any(o => o.Oid == fiv.ToString()), $"Missing policy {fiv}");
            }
        }

        /// <summary>
        /// Test that the service gets all policies
        /// </summary>
        [TestMethod]
        public void TestGetRolePolicies()
        {

            var ipoli = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();

            Assert.IsNotNull(ipoli);

            var policies = ipoli.GetActivePolicies(new SecurityRole() { Name = "Administrators", Key = Guid.Parse("f6d2ba1d-5bb5-41e3-b7fb-2ec32418b2e1") });
            Assert.AreNotEqual(0, policies.Count());

        }

        /// <summary>
        /// Test that the service gets all policies
        /// </summary>
        [TestMethod]
        public void TestGetUserPolicies()
        {

            var ipoli = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();

            Assert.IsNotNull(ipoli);

            var policies = ipoli.GetActivePolicies(new GenericIdentity("system"));
            Assert.AreNotEqual(0, policies.Count());

        }

    }
}
