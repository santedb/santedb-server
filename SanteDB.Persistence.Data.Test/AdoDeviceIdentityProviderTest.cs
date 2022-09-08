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
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Device identity provider test
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class AdoDeviceIdentityProviderTest : DataPersistenceTest
    {
        /// <summary>
        /// Verifies that a device entity can be persisted properly
        /// </summary>
        [Test]
        public void TestCreateDeviceIdentity()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_DEV_001"));

            // Create the device identity
            var appIdentity = serviceProvider.CreateIdentity("TEST_DEV_001", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(appIdentity);
            Assert.AreEqual("TEST_DEV_001", appIdentity.Name);
            Assert.IsFalse(appIdentity.IsAuthenticated);

            // When creating an identity the policies in group DeviceS are automatically applied to the client
        }

        /// <summary>
        /// Tests that a regular principal cannot create
        /// </summary>
        [Test]
        public void TestNonPrivCannotCreate()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_DEV_002"));

            // Create the device identity
            try
            {
                var appIdentity = serviceProvider.CreateIdentity("TEST_DEV_002", "THIS_IS_A_SECRET", AuthenticationContext.AnonymousPrincipal);
                Assert.Fail("Anonymous should not be able to create a device identity");
            }
            catch (PolicyViolationException e)
            {
                Assert.AreEqual(PermissionPolicyIdentifiers.CreateDevice, e.PolicyId);
            }
            catch (Exception)
            {
                Assert.Fail("Should throw a policy violation exception");
            }
        }

        /// <summary>
        /// Test that policies from skel app group are copied over
        /// </summary>
        [Test]
        public void TestSkelPoliciesCopied()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            var pipService = ApplicationServiceContext.Current.GetService<IPolicyInformationService>();

            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_DEV_003"));

            // Create the device identity
            var appIdentity = serviceProvider.CreateIdentity("TEST_DEV_003", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(appIdentity);
            Assert.AreEqual("TEST_DEV_003", appIdentity.Name);
            Assert.IsFalse(appIdentity.IsAuthenticated);

            // When creating an identity the policies in group DeviceS are automatically applied to the client
            var appPolicies = pipService.GetPolicies(appIdentity);
            Assert.IsTrue(appPolicies.Any(o => o.Policy.Oid == PermissionPolicyIdentifiers.Login));
            Assert.IsTrue(appPolicies.Any(o => o.Policy.Oid == PermissionPolicyIdentifiers.ReadMetadata));
        }

        /// <summary>
        /// Ensures that the Device identity provider
        /// </summary>
        [Test]
        public void TestAuthenticateSuccess()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_DEV_004"));

            // Create the device identity
            serviceProvider.CreateIdentity("TEST_DEV_004", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);

            // Authenticate
            try
            {
                var principal = serviceProvider.Authenticate("TEST_DEV_004", "THIS_IS_A_SECRET");
                Assert.IsInstanceOf<IDeviceIdentity>(principal.Identity);
                Assert.AreEqual("TEST_DEV_004", principal.Identity.Name);
            }
            catch
            {
                Assert.Fail("Authentication failed");
            }
        }

        /// <summary>
        /// Ensures that after a series of invalid login attempts the Device account is locked
        /// </summary>
        [Test]
        public void TestAppIsLockedOut()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_DEV_005"));

            // Create the device identity
            serviceProvider.CreateIdentity("TEST_DEV_005", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);

            // Authenticate
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    serviceProvider.Authenticate("TEST_DEV_005", "THIS_IS_A_SECRET_BUT_NOT_THE_SECRET");
                    Assert.Fail("Authentication should have failed");
                }
                catch // ignore login fail
                {
                }
            }

            // Attempt final authentication
            try
            {
                serviceProvider.Authenticate("TEST_DEV_005", "THIS_IS_A_SECRET");
                Assert.Fail("Authentication should have failed");
            }
            catch (AuthenticationException e) when (e.Message == this.m_localizationService.GetString(ErrorMessageStrings.AUTH_DEV_LOCKED))
            {
            }
            catch (Exception e)
            {
                Assert.Fail($"Improper authentication error - Expected {typeof(AuthenticationException)} but got {e.GetType()}");
            }
        }

        /// <summary>
        /// Ensures that the Device identity provider
        /// </summary>
        [Test]
        public void TestChangeOwnPass()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_DEV_006"));

            // Create the device identity
            serviceProvider.CreateIdentity("TEST_DEV_006", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);

            // Authenticate
            try
            {
                var principal = serviceProvider.Authenticate("TEST_DEV_006", "THIS_IS_A_SECRET");
                serviceProvider.ChangeSecret("TEST_DEV_006", "THIS_IS_A_NEW_SECRET", principal);
                try
                {
                    principal = serviceProvider.Authenticate("TEST_DEV_006", "THIS_IS_A_NEW_SECRET");
                }
                catch (AuthenticationException)
                {
                    Assert.Fail("Password changed but couldn't auth");
                }

                try
                {
                    serviceProvider.Authenticate("TEST_DEV_006", "THIS_IS_A_SECRET"); // Should fail
                    Assert.Fail("Should have thrown exception");
                }
                catch
                {
                }
            }
            catch
            {
                Assert.Fail("Authentication failed");
            }
        }

        /// <summary>
        /// Tests that the lockout is set and cleared
        /// </summary>
        [Test]
        public void TestSetLockout()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_DEV_008"));

            // Create the device identity
            serviceProvider.CreateIdentity("TEST_DEV_008", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);

            try
            {
                serviceProvider.Authenticate("TEST_DEV_008", "THIS_IS_A_SECRET");
            }
            catch
            {
                Assert.Fail("Authentication failed");
            }

            // Lockout
            serviceProvider.SetLockout("TEST_DEV_008", true, AuthenticationContext.SystemPrincipal);
            try
            {
                serviceProvider.Authenticate("TEST_DEV_008", "THIS_IS_A_SECRET");
                Assert.Fail("Lockout should be active");
            }
            catch (AuthenticationException e) when (e.Message == this.m_localizationService.GetString(ErrorMessageStrings.AUTH_DEV_LOCKED))
            {
            }
            catch (Exception e)
            {
                Assert.Fail($"Wrong exception thrown - expected {typeof(AuthenticationException)} but got {e.GetType()}");
            }

            // Clear lockout
            serviceProvider.SetLockout("TEST_DEV_008", false, AuthenticationContext.SystemPrincipal);
            try
            {
                serviceProvider.Authenticate("TEST_DEV_008", "THIS_IS_A_SECRET");
            }
            catch (Exception e)
            {
                Assert.Fail("Should have authenticated");
            }
        }

        /// <summary>
        /// Tests that the security layer does not permit unauthenticated principals from performing operations
        /// </summary>
        [Test]
        public void TestShouldNotAllowUnauthenticatedIdentity()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_DEV_012"));

            // Create the device identity
            serviceProvider.CreateIdentity("TEST_DEV_012", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);
            var identity = serviceProvider.GetIdentity("TEST_DEV_012");
            var principal = new GenericPrincipal(identity, new string[] { "ADMINISTRATORS" });

            try
            {
                serviceProvider.ChangeSecret("TEST_DEV_012", "NOT_ALLOWED", principal);
                Assert.Fail("Should have blocked unauthenticated secret change");
            }
            catch
            {
            }

            try
            {
                serviceProvider.CreateIdentity("TEST_DEV_XXX", "NOT_ALLOWED", principal);
                Assert.Fail("Should have blocked unauthenticated create");
            }
            catch
            {
            }
        }

        /// <summary>
        /// Ensures that authentication can be cancelled by a plugin
        /// </summary>
        [Test]
        public void TestShouldPreventAuthentication()
        {
            // Create the device identity
            var serviceProvider = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            serviceProvider.CreateIdentity("TEST_DEV_013", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);

            serviceProvider.Authenticating += (o, e) =>
            {
                e.Cancel = (e.UserName == "TEST_DEV_013");
                e.Success = !e.Cancel;
            };

            try
            {
                serviceProvider.Authenticate("TEST_DEV_013", "THIS_IS_A_SECRET");
                Assert.Fail("Should have cancelled authentication");
            }
            catch (AuthenticationException e) when (e.Message == this.m_localizationService.GetString(ErrorMessageStrings.AUTH_CANCELLED))
            {
            }
            catch (Exception e)
            {
                Assert.Fail($"Incorrect exception thrown expected {typeof(AuthenticationException)} but got {e.GetType()}");
            }
        }
    }
}