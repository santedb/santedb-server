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

using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using System.Security.Authentication;

namespace SanteDB.Persistence.Data.ADO.Tests
{
    [ExcludeFromCodeCoverage]
    [TestFixture(Category = "Persistence")]
    public class AdoIdentityProviderTest : DataTest
    {

        /// <summary>
        /// Class startup
        /// </summary>
        /// <param name="context"></param>
        [SetUp]
        public void ClassSetup()
        {

            IPasswordHashingService hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
            AuthenticationContext.EnterSystemContext();
            var dataService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            if (dataService.GetIdentity("admin@identitytest.com") == null)
                dataService.CreateIdentity("admin@identitytest.com", "password", AuthenticationContext.Current.Principal);
            if (dataService.GetIdentity("user@identitytest.com") == null)
                dataService.CreateIdentity("user@identitytest.com", "password", AuthenticationContext.Current.Principal);

            IRoleProviderService roleService = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            roleService.AddUsersToRoles(new string[] { "admin@identitytest.com", "user@identitytest.com" }, new string[] { "USERS" }, AuthenticationContext.Current.Principal);
            roleService.AddUsersToRoles(new string[] { "admin@identitytest.com" }, new string[] { "ADMINISTRATORS" }, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Tests that the authenticate method successfully authenticates a user
        /// </summary>
        [Test]
        public void TestAuthenticateSuccess()
        {

            IIdentityProviderService provider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var principal = provider.Authenticate("admin@identitytest.com", "password");
            Assert.AreEqual("admin@identitytest.com", principal.Identity.Name);
            Assert.IsTrue(principal.Identity.IsAuthenticated);
            Assert.AreEqual("LOCAL", principal.Identity.AuthenticationType);

        }

        /// <summary>
        /// Tests that the authenticate method successfully retrieves a non-authenticated identity
        /// </summary>
        [Test]
        public void TestGetNonAuthenticatedPrincipal()
        {

            IIdentityProviderService provider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var identity = provider.GetIdentity("admin@identitytest.com");
            Assert.AreEqual("admin@identitytest.com", identity.Name);
            Assert.IsFalse(identity.IsAuthenticated);
            Assert.IsNull(identity.AuthenticationType);

        }
        /// <summary>
        /// Tests that the authenticate method successfully logs an invalid login attempt
        /// </summary>
        [Test]
        public void TestInvalidLoginAttemptCount()
        {

            var dataPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityUser>>();
            IIdentityProviderService provider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();

            // Reset data for test
            //var user = provider..Query(u => u.UserName == "user@identitytest.com", null).First();
            //user.Lockout = null;
            //user.LastLoginTime = null;
            //user.InvalidLoginAttempts = 0;
            //dataPersistence.Update(user, provider.Authenticate("admin@identitytest.com", "password"), TransactionMode.Commit);

            try
            {

                var principal = provider.Authenticate("user@identitytest.com", "passwordz");
                Assert.Fail("Should throw SecurityException");
            }
            catch (AuthenticationException)
            {
                // We should have a lockout
                //user = dataPersistence.Get(user.Id(), null, false);
                //Assert.AreEqual(1, user.InvalidLoginAttempts);
                //Assert.AreEqual(null, user.LastLoginTime);
                //Assert.IsFalse(user.Lockout.HasValue);

            }

        }


        /// <summary>
        /// Tests that the authenticate method successfully locks a user account after three attempts
        /// </summary>
        [Test]
        public void TestAuthenticateLockout()
        {

            //var dataPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityUser>>();
            IIdentityProviderService provider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            // Reset data for test
            //var user = dataPersistence.Query(u => u.UserName == "user@identitytest.com", null).First();
            //user.Lockout = null;
            // user.LastLoginTime = null;
            //user.InvalidLoginAttempts = 0;
            //dataPersistence.Update(user, TransactionMode.Commit);


            // Try 4 times to log in
            for (int i = 0; i < 7; i++)
                try
                {
                    var principal = provider.Authenticate("user@identitytest.com", "passwordz");
                    Assert.Fail("Should throw SecurityException");
                }
                catch (AuthenticationException)
                { }

            // We should have a lockout
            //user = dataPersistence.Get(user.Id(), null, false);
            //Assert.IsTrue(user.InvalidLoginAttempts >= 4);
            //Assert.AreEqual(null, user.LastLoginTime);
            //Assert.IsTrue(user.Lockout.HasValue);


        }

        /// <summary>
        /// Tests that the authenticate method successfully authenticates a user
        /// </summary>
        [Test]
        public void TestAuthenticateFailure()
        {
            try
            {
                IIdentityProviderService provider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
                var principal = provider.Authenticate("admin@identitytest.com", "passwordz");
                Assert.Fail("Should throw SecurityException");
            }
            catch (AuthenticationException)
            { }
        }

        /// <summary>
        /// Tests that the identity provider can change passwords
        /// </summary>
        [Test]
        public void TestChangePassword()
        {

            // var dataPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityUser>>();
            IIdentityProviderService identityProvider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            //var user = dataPersistence.Query(u => u.UserName == "admin@identitytest.com", null).First();
            // var existingPassword = user.PasswordHash;

            // Now change the password
            var principal = identityProvider.Authenticate("admin@identitytest.com", "password");
            identityProvider.ChangePassword("admin@identitytest.com", "newpassword", principal);
            principal = identityProvider.Authenticate("admin@identitytest.com", "newpassword");
            identityProvider.ChangePassword("admin@identitytest.com", "password", principal);
            //user = dataPersistence.Get(user.Id(), principal, false);
            //Assert.AreNotEqual(existingPassword, user.PasswordHash);

            // Change the password back 
            //user.PasswordHash = existingPassword;
            //dataPersistence.Update(user, principal, TransactionMode.Commit);
        }

        /// <summary>
        /// Tests that anonymous user creation works
        /// </summary>
        [Test]
        public void TestAnonymousUserCreation()
        {

            var dataPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityUser>>();
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

            var identity = identityService.CreateIdentity("anonymous@identitytest.com", "mypassword", AuthenticationContext.Current.Principal);
            Assert.IsNotNull(identity);
            Assert.IsFalse(identity.IsAuthenticated);

            // Now verify with data persistence
            //var dataUser = dataPersistence.Query(u => u.UserName == "anonymous@identitytest.com", null).First();
            //Assert.AreEqual(hashingService.ComputeHash("mypassword"), dataUser.PasswordHash);
        }

        /// <summary>
        /// Tests that administrative user creation works
        /// </summary>
        [Test]
        public void TestAdministrativeUserCreation()
        {

            // var dataPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityUser>>();
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

            using (AuthenticationContext.EnterContext(identityService.Authenticate("admin@identitytest.com", "password")))
            {
                var identity = identityService.CreateIdentity("admincreated@identitytest.com", "mypassword", AuthenticationContext.Current.Principal);
                Assert.IsNotNull(identity);
                Assert.IsFalse(identity.IsAuthenticated);
            }

            // Now verify with data persistence
            //var dataUser = dataPersistence.Query(u => u.UserName == "admincreated@identitytest.com", null).First();
            //Assert.AreEqual(hashingService.ComputeHash("mypassword"), dataUser.PasswordHash);
            //Assert.IsFalse(dataUser.Lockout.HasValue);
            //Assert.AreEqual(authContext.Identity.Name, dataUser.CreatedBy.UserName);


        }
    }
}
