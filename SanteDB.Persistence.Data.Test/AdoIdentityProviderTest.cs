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
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// ADO identity provider
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class AdoIdentityProviderTest : DataPersistenceTest
    {
        /// <summary>
        /// Ensures that we can reate an identity
        /// </summary>
        [Test]
        public void TestCreateIdentity()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            Assert.IsNull(service.GetIdentity("TEST_01"));
            var identity = service.CreateIdentity("TEST_01", "@TESTPa$$w0rd", adminPrincipal);
            Assert.AreEqual("TEST_01", identity.Name);
            Assert.IsNotNull(service.GetIdentity("TEST_01"));
        }

        /// <summary>
        /// Test authentication
        /// </summary>
        [Test]
        public void TestAuthenticate()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNull(service.GetIdentity("TEST_02"));
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            var identity = service.CreateIdentity("TEST_02", "@TESTPa$$w0rd", adminPrincipal);

            var pcpl = service.Authenticate("TEST_02", "@TESTPa$$w0rd");
            Assert.AreEqual("TEST_02", pcpl.Identity.Name);
            Assert.AreEqual(true, pcpl.Identity.IsAuthenticated);
        }

        /// <summary>
        /// Set the lockout on the user after X failed attempts
        /// </summary>
        [Test]
        public void TestLockoutAfterInvalidCount()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNull(service.GetIdentity("TEST_03"));
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            var identity = service.CreateIdentity("TEST_03", "@TESTPa$$w0rd", adminPrincipal);

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var pcpl = service.Authenticate("TEST_03", "INVALID");
                    Assert.Fail("Should not login");
                }
                catch { }
            }

            try
            {
                var pcpl = service.Authenticate("TEST_03", "@TESTPa$$w0rd");
                Assert.Fail("Should not authenticate");
            }
            catch (AuthenticationException e) when (e.Message == this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_LOCKED))
            { }
            catch (Exception e) { Assert.Fail($"Invalid exception thrown. Expected AuthenticationException with message {ErrorMessageStrings.AUTH_USR_LOCKED} but got {e.GetType().Name} with message {e.Message}"); }

        }

        /// <summary>
        /// Ensures that someone can be locked and unlocked
        /// </summary>
        [Test]
        public void TestLockout()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNull(service.GetIdentity("TEST_04"));
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            var identity = service.CreateIdentity("TEST_04", "@TESTPa$$w0rd", adminPrincipal);

            var principal = service.Authenticate("TEST_04", "@TESTPa$$w0rd");
            // Lockout
            service.SetLockout("TEST_04", true, adminPrincipal);
            // Shouldn't login
            try
            {
                service.Authenticate("TEST_04", "@TESTPa$$w0rd");
            }
            catch (AuthenticationException e) when (e.Message == this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_LOCKED))
            {
            }
            catch (Exception e) { Assert.Fail($"Invalid exception thrown. Expected AuthenticationException with message {ErrorMessageStrings.AUTH_USR_LOCKED} but got {e.GetType().Name} with message {e.Message}"); }


            service.SetLockout("TEST_04", false, adminPrincipal);
            service.Authenticate("TEST_04", "@TESTPa$$w0rd");
        }

        /// <summary>
        /// Test we can remove the identity
        /// </summary>
        [Test]
        public void TestRemoveIdentity()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNull(service.GetIdentity("TEST_05"));
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            var identity = service.CreateIdentity("TEST_05", "@TESTPa$$w0rd", adminPrincipal);

            // Should auth
            var principal = service.Authenticate("TEST_05", "@TESTPa$$w0rd");
            service.DeleteIdentity("TEST_05", adminPrincipal);
            // Should not auth
            try
            {
                principal = service.Authenticate("TEST_05", "@TESTPa$$w0rd");
                Assert.Fail("Should not login");
            }
            catch (AuthenticationException e) when (e.Message == this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_INVALID))
            {
            }
            catch (Exception e) { Assert.Fail($"Invalid exception thrown. Expected AuthenticationException with message {ErrorMessageStrings.AUTH_USR_INVALID} but got {e.GetType().Name} with message {e.Message}"); }

        }

        /// <summary>
        /// Test changing of own password
        /// </summary>
        [Test]
        public void TestChangePassword()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNull(service.GetIdentity("TEST_06"));
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            var identity = service.CreateIdentity("TEST_06", "@TESTPa$$w0rd", adminPrincipal);

            var principal = service.Authenticate("TEST_06", "@TESTPa$$w0rd");
            service.ChangePassword("TEST_06", "4$1mpl3P4wrE", principal);

            // Test can't login with previous
            try
            {
                principal = service.Authenticate("TEST_06", "@TESTPa$$w0rd");
                Assert.Fail("Should not have logged in");
            }
            catch (AuthenticationException e) when (e.Message == this.m_localizationService.GetString(ErrorMessageStrings.AUTH_USR_INVALID))
            { }
            catch (Exception e) { Assert.Fail($"Invalid exception thrown. Expected AuthenticationException with message {ErrorMessageStrings.AUTH_USR_INVALID} but got {e.GetType().Name} with message {e.Message}"); }

            principal = service.Authenticate("TEST_06", "4$1mpl3P4wrE");
        }

        /// <summary>
        /// Test that user A cannot change user B password
        /// </summary>
        [Test]
        public void TestCannotChangeAnothers()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNull(service.GetIdentity("TEST_07A"));
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            var identity = service.CreateIdentity("TEST_07A", "@TESTPa$$w0rd", adminPrincipal);
            identity = service.CreateIdentity("TEST_07B", "@TESTPa$$w0rd", adminPrincipal);

            var principal = service.Authenticate("TEST_07A", "@TESTPa$$w0rd");
            try
            {
                service.ChangePassword("TEST_07B", "4$1mpl3P4wrE", principal);
                Assert.Fail("Should have thrown exception");
            }
            catch (PolicyViolationException e) when (e.PolicyId == PermissionPolicyIdentifiers.ChangePassword)
            {
            }
            catch (Exception e) { Assert.Fail($"Invalid exception thrown. Expected PolicyViolationException but got {e.GetType().Name} with message {e.Message}"); }


            principal = service.Authenticate("TEST_07B", "@TESTPa$$w0rd");
        }

        /// <summary>
        /// Test that the provider adds a claim
        /// </summary>
        [Test]
        public void TestAddClaim()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNull(service.GetIdentity("TEST_08"));
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            var identity = service.CreateIdentity("TEST_08", "@TESTPa$$w0rd", adminPrincipal);

            // Add a claim
            service.AddClaim("TEST_08", new SanteDBClaim("test", "TEST"), adminPrincipal);
            var claimsIdentity = service.GetIdentity("TEST_08") as IClaimsIdentity;
            Assert.AreEqual("TEST", claimsIdentity.FindFirst("test")?.Value);
        }

        /// <summary>
        /// Test that the provider removes a claim
        /// </summary>
        [Test]
        public void TestRemoveClaim()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNull(service.GetIdentity("TEST_09"));
            var adminPrincipal = service.Authenticate("administrator", "Mohawk123");
            var identity = service.CreateIdentity("TEST_09", "@TESTPa$$w0rd", adminPrincipal);

            // Add a claim
            service.AddClaim("TEST_09", new SanteDBClaim("test", "TEST"), AuthenticationContext.SystemPrincipal);
            var claimsIdentity = service.GetIdentity("TEST_09") as IClaimsIdentity;
            Assert.AreEqual("TEST", claimsIdentity.FindFirst("test")?.Value);
            service.RemoveClaim("TEST_09", "test", AuthenticationContext.SystemPrincipal);
            claimsIdentity = service.GetIdentity("TEST_09") as IClaimsIdentity;
            Assert.IsNull(claimsIdentity.FindFirst("test"));
        }

        /// <summary>
        /// Test get sid
        /// </summary>
        [Test]
        public void TestGetSid()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.AreEqual(AuthenticationContext.SystemUserSid, service.GetSid("SYSTEM").ToString());
        }

        /// <summary>
        /// Get get an identity
        /// </summary>
        [Test]
        public void TestGetIdentity()
        {
            var service = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.AreEqual("SYSTEM", service.GetIdentity("SYSTEM").Name);
            Assert.IsFalse(service.GetIdentity("SYSTEM").IsAuthenticated);
            Assert.AreEqual("SYSTEM", service.GetIdentity(Guid.Parse(AuthenticationContext.SystemUserSid)).Name);
        }
    }
}