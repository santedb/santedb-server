using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Test ADO policy information service.
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO Policy Provider")]

    public class AdoPolicyInformationTest : DataPersistenceTest
    {

        /// <summary>
        /// tests that the PIP can get call policies
        /// </summary>
        [Test]
        public void TestGetAllPolicies()
        {
            var pipService = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();
            var policies = pipService.GetPolicies();
            Assert.IsTrue(policies.Any(p => p.Oid == PermissionPolicyIdentifiers.Login));
            Assert.IsTrue(policies.Any(p => p.Oid == PermissionPolicyIdentifiers.UnrestrictedAdministration));
            Assert.IsTrue(policies.Any(p => p.Oid == PermissionPolicyIdentifiers.ReadClinicalData));
            Assert.IsTrue(policies.Any(p => p.Oid == PermissionPolicyIdentifiers.ReadMetadata));
        }

        /// <summary>
        /// Test getting policies for role
        /// </summary>
        [Test]
        public void TestGetPoliciesForPrincipal()
        {
            var pipService = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();
            var authService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

            var principal = authService.Authenticate("Administrator", "Mohawk123");
            var principalPolicies = pipService.GetPolicies(principal);
            Assert.IsTrue(principalPolicies.Where(o => o.Policy.Oid == PermissionPolicyIdentifiers.UnrestrictedClinicalData).Any(r => r.Rule == Core.Model.Security.PolicyGrantType.Deny));

        }

        /// <summary>
        /// Test getting policies for role
        /// </summary>
        [Test]
        public void TestGetPolicyInstanceForPrincipal()
        {
            var pipService = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();
            var authService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

            var principal = authService.Authenticate("Administrator", "Mohawk123");
            var principalPolicies = pipService.GetPolicyInstance(principal, PermissionPolicyIdentifiers.UnrestrictedClinicalData);
            Assert.AreEqual(PolicyGrantType.Deny, principalPolicies.Rule);

        }

        // TODO: Other get policies for ACT and ENTITY

        // TODO: Other add policies for ACT and ENTITY
    }
}
