/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
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
