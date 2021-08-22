using SanteDB.Core;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.TestFramework;
using System;
using System.Linq;
using System.Security.Principal;
using NUnit.Framework;

namespace SanteDB.Persistence.Data.ADO.Tests
{
    [TestFixture(Category = "Persistence")]
    public class AdoPolicyProviderTest : DataTest
    {

        /// <summary>
        /// Test that the service gets all policies
        /// </summary>
        [Test]
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
                if(!fiv.Equals( PermissionPolicyIdentifiers.AlterLocalIdentity) &&
                    !fiv.Equals(PermissionPolicyIdentifiers.CreateLocalIdentity))
                    Assert.IsTrue(policies.Any(o => o.Oid == fiv.ToString()), $"Missing policy {fiv}");
            }
        }

        /// <summary>
        /// Test that the service gets all policies
        /// </summary>
        [Test]
        public void TestGetRolePolicies()
        {

            var ipoli = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();

            Assert.IsNotNull(ipoli);

            var policies = ipoli.GetPolicies(new SecurityRole() { Name = "Administrators", Key = Guid.Parse("f6d2ba1d-5bb5-41e3-b7fb-2ec32418b2e1") });
            Assert.AreNotEqual(0, policies.Count());

        }

        /// <summary>
        /// Test that the service gets all policies
        /// </summary>
        [Test]
        public void TestGetUserPolicies()
        {

            var ipoli = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();

            Assert.IsNotNull(ipoli);

            var policies = ipoli.GetPolicies(new GenericIdentity("system"));
            Assert.AreNotEqual(0, policies.Count());

        }

    }
}
