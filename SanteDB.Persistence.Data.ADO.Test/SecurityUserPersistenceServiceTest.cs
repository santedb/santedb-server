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

using System;
using System.Linq;
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;

namespace SanteDB.Persistence.Data.ADO.Test
{
    /// <summary>
    /// Summary description for SecurityUserPersistenceServiceTest
    /// </summary>
    [TestFixture(Category = "Persistence")]
    public class SecurityUserPersistenceServiceTest : PersistenceTest<SecurityUser>
    {

        [SetUp]
        public void ClassSetup()
        {
            AuthenticationContext.EnterSystemContext();


        }

        /// <summary>
        /// Test the insertion of a valid security user
        /// </summary>
        [Test]
        public void TestInsertValidSecurityUser()
        {

            SecurityUser userUnderTest = new SecurityUser()
            {
                Email = "admin@test.com",
                EmailConfirmed = true,
                Password = "test_user_hash_store",
                SecurityHash = "test_security_hash",
                UserName = "admin",
                UserClass = UserClassKeys.HumanUser
            };

            var userAfterTest = base.DoTestInsert(userUnderTest);
            Assert.AreEqual(userUnderTest.UserName, userAfterTest.UserName);
        }
        
        /// <summary>
        /// Test the updating of a valid security user
        /// </summary>
        [Test]
        public void TestUpdateValidSecurityUser()
        {

            IPasswordHashingService hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

            SecurityUser userUnderTest = new SecurityUser()
            {
                Email = "update@test.com",
                EmailConfirmed = false,
                Password = hashingService.ComputeHash("password"),
                SecurityHash = "cert",
                UserName = "updateTest",
                UserClass = UserClassKeys.HumanUser

            };
            
            // Store user
            IIdentityProviderService identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var authContext = AuthenticationContext.SystemPrincipal;
            Assert.IsNotNull(authContext);
            var userAfterUpdate = base.DoTestUpdate(userUnderTest, "PhoneNumber", authContext);

            // Update
            Assert.IsNotNull(userAfterUpdate.UpdatedTime);
            Assert.IsNotNull(userAfterUpdate.PhoneNumber);
            Assert.AreEqual(authContext.Identity.Name, userAfterUpdate.LoadProperty<SecurityProvenance>("UpdatedBy").LoadProperty<SecurityUser>("User").UserName);
        }

        /// <summary>
        /// Test valid query result
        /// </summary>
        [Test]
        public void TestQueryValidResult()
        {

            IPasswordHashingService hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
            String securityHash = Guid.NewGuid().ToString();
            SecurityUser userUnderTest = new SecurityUser()
            {
                Email = "query@test.com",
                EmailConfirmed = false,
                Password = hashingService.ComputeHash("password"),
                SecurityHash = securityHash,
                UserName = "queryTest",
                UserClass = UserClassKeys.HumanUser

            };

            var testUser = base.DoTestInsert(userUnderTest);
            IIdentityProviderService identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var results = base.DoTestQuery(o => o.Email == "query@test.com", testUser.Key);
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(userUnderTest.Email, results.First().Email);
        }

        /// <summary>
        /// Tests the delay loading of properties works
        /// </summary>
        [Test]
        public void TestDelayLoadUserProperties()
        {
            IPasswordHashingService hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
            String securityHash = Guid.NewGuid().ToString();
            SecurityUser userUnderTest = new SecurityUser()
            {
                Email = "delay@test.com",
                EmailConfirmed = false,
                Password = hashingService.ComputeHash("password"),
                SecurityHash = securityHash,
                UserName = "delayLoadTest",
                UserClass = UserClassKeys.HumanUser
            };


            var userAfterInsert = base.DoTestInsert(userUnderTest, null);
            var roleProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            var identityProvider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

            // Allow login
            roleProvider.AddUsersToRoles(new string[] { "delayLoadTest" },  new string[] { "USERS" },  AuthenticationContext.Current.Principal);
            roleProvider.AddUsersToRoles(new string[] { "delayLoadTest" },  new string[] { "ADMINISTRATORS" },  AuthenticationContext.Current.Principal);

            var auth = identityProvider.Authenticate("delayLoadTest", "password");
            roleProvider.CreateRole("TestDelayLoadUserPropertiesGroup", auth);
            roleProvider.AddUsersToRoles(new String[] { "delayLoadTest" },  new String[] { "TestDelayLoadUserPropertiesGroup" }, AuthenticationContext.Current.Principal);

            // Now trigger a delay load
            var userForTest = base.DoTestQuery(u => u.UserName == "delayLoadTest", userAfterInsert.Key, auth).First();
            Assert.AreEqual(3, userForTest.Roles.Count);
            Assert.IsTrue(userForTest.Roles.Exists(o=>o.Name == "TestDelayLoadUserPropertiesGroup"));


        }

    }
}
