/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Test ADO policy information service.
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
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

        /// <summary>
        /// Test adding a policy to an entity
        /// </summary>
        [Test]
        public void TestAddPolicyToEntity()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var entity = new Entity()
                {
                    ClassConceptKey = EntityClassKeys.Food,
                    DeterminerConceptKey = DeterminerKeys.Specific,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.OfficialRecord, "TEST")
                    }
                };

                var afterInsert = base.TestInsert(entity);
                // Add a policy
                var pipService = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();
                var pdpService = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();
                var authService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

                pipService.AddPolicies(afterInsert, PolicyGrantType.Grant, AuthenticationContext.SystemPrincipal, PermissionPolicyIdentifiers.UnrestrictedClinicalData);

                // Re-fetch and check policy count
                Assert.AreEqual(1, pipService.GetPolicies(afterInsert).Count());
                var afterQuery = base.TestQuery<Entity>(o => o.Key == afterInsert.Key, 1).AsResultSet().First();
                Assert.AreEqual(1, afterQuery.Policies.Count);

                // Attempt to access ?
                var principal = authService.Authenticate("Administrator", "Mohawk123");
                pdpService.GetPolicyDecision(principal, afterQuery);
            }
        }

        // TODO: Add policies to a security device

        // TODO: Remove policies from object

        // TODO: Other get policies for ACT and ENTITY

        // TODO: Other add policies for ACT and ENTITY
    }
}