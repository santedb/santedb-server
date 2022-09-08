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
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// ADO Policy Information Service Test Suite
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class AdoSessionProviderTest : DataPersistenceTest
    {
        /// <summary>
        /// Test the establishing of a session
        /// </summary>
        [Test]
        public void TestEstablishSession()
        {
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var appService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            var devService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            var sesService = ApplicationServiceContext.Current.GetService<ISessionProviderService>();

            // Create all items
            identityService.CreateIdentity("TEST_USER_SES_01", "@P4SSW0r$", AuthenticationContext.SystemPrincipal);
            appService.CreateIdentity("TEST_APP_SES_01", "TEST_APP_SECRET", AuthenticationContext.SystemPrincipal);
            devService.CreateIdentity("TEST_DEV_SES_01", "TEST_DEV_SECRET", AuthenticationContext.SystemPrincipal);

            // Authentication
            var userPrincipal = identityService.Authenticate("TEST_USER_SES_01", "@P4SSW0r$");
            var appPrincipal = appService.Authenticate("TEST_APP_SES_01", "TEST_APP_SECRET");
            var devPrincipal = devService.Authenticate("TEST_DEV_SES_01", "TEST_DEV_SECRET");

            // Combo principal
            var sesPrincipal = new SanteDBClaimsPrincipal(new IIdentity[] { userPrincipal.Identity, appPrincipal.Identity, devPrincipal.Identity });
            var session = sesService.Establish(sesPrincipal, "localhost", false, null, new string[] { "*" }, "en");
            Assert.IsNotNull(session.RefreshToken);
            Assert.IsNotNull(session.Id);
            Assert.IsTrue(session.Claims.Count(o => o.Type == SanteDBClaimTypes.SanteDBScopeClaim) > 2);
        }

        /// <summary>
        /// Test that authentication via session can be performed
        /// </summary>
        [Test]
        public void TestAuthenticateSession()
        {
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var appService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            var devService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            var sesService = ApplicationServiceContext.Current.GetService<ISessionProviderService>();
            var sesIdService = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>();
            // Create all items
            identityService.CreateIdentity("TEST_USER_SES_02", "@P4SSW0r$", AuthenticationContext.SystemPrincipal);
            appService.CreateIdentity("TEST_APP_SES_02", "TEST_APP_SECRET", AuthenticationContext.SystemPrincipal);
            devService.CreateIdentity("TEST_DEV_SES_02", "TEST_DEV_SECRET", AuthenticationContext.SystemPrincipal);

            // Authentication
            var userPrincipal = identityService.Authenticate("TEST_USER_SES_02", "@P4SSW0r$");
            var appPrincipal = appService.Authenticate("TEST_APP_SES_02", "TEST_APP_SECRET");
            var devPrincipal = devService.Authenticate("TEST_DEV_SES_02", "TEST_DEV_SECRET");

            // Combo principal
            var tPrincipal = new SanteDBClaimsPrincipal(new IIdentity[] { userPrincipal.Identity, appPrincipal.Identity, devPrincipal.Identity });
            var session = sesService.Establish(tPrincipal, "localhost", false, null, new string[] { "*" }, "en");

            // Attempt to auth via session
            var sesPrincipal = sesIdService.Authenticate(session) as IClaimsPrincipal;
            Assert.AreEqual(userPrincipal.Identity.Name, sesPrincipal.Identity.Name);
            Assert.IsTrue(sesPrincipal.Identities.OfType<IDeviceIdentity>().Any());
            Assert.IsTrue(sesPrincipal.Identities.OfType<IApplicationIdentity>().Any());
            Assert.IsTrue(sesPrincipal.Identities.OfType<IApplicationIdentity>().Any());
            Assert.IsNotNull(sesPrincipal.FindFirst(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim));
            Assert.IsNotNull(sesPrincipal.FindFirst(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim));
            Assert.AreEqual(3, sesPrincipal.FindAll(SanteDBClaimTypes.Sid).Count());
            Assert.AreEqual(userPrincipal.Identity.Name, sesPrincipal.FindFirst(SanteDBClaimTypes.Name).Value);

            // Should hit cache
            sesPrincipal = sesIdService.Authenticate(session) as IClaimsPrincipal;
            Assert.AreEqual(userPrincipal.Identity.Name, sesPrincipal.Identity.Name);
            Assert.IsTrue(sesPrincipal.Identities.OfType<IDeviceIdentity>().Any());
            Assert.IsTrue(sesPrincipal.Identities.OfType<IApplicationIdentity>().Any());
            Assert.IsTrue(sesPrincipal.Identities.OfType<IApplicationIdentity>().Any());
            Assert.IsNotNull(sesPrincipal.FindFirst(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim));
            Assert.IsNotNull(sesPrincipal.FindFirst(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim));
            Assert.AreEqual(3, sesPrincipal.FindAll(SanteDBClaimTypes.Sid).Count());
            Assert.AreEqual(userPrincipal.Identity.Name, sesPrincipal.FindFirst(SanteDBClaimTypes.Name).Value);


        }

        /// <summary>
        /// Test the establishing of a session
        /// </summary>
        [Test]
        public void TestExtendSession()
        {
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var appService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            var devService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            var sesService = ApplicationServiceContext.Current.GetService<ISessionProviderService>();

            // Create all items
            identityService.CreateIdentity("TEST_USER_SES_03", "@P4SSW0r$", AuthenticationContext.SystemPrincipal);
            appService.CreateIdentity("TEST_APP_SES_03", "TEST_APP_SECRET", AuthenticationContext.SystemPrincipal);
            devService.CreateIdentity("TEST_DEV_SES_03", "TEST_DEV_SECRET", AuthenticationContext.SystemPrincipal);

            // Authentication
            var userPrincipal = identityService.Authenticate("TEST_USER_SES_03", "@P4SSW0r$");
            var appPrincipal = appService.Authenticate("TEST_APP_SES_03", "TEST_APP_SECRET");
            var devPrincipal = devService.Authenticate("TEST_DEV_SES_03", "TEST_DEV_SECRET");

            // Combo principal
            var sesPrincipal = new SanteDBClaimsPrincipal(new IIdentity[] { userPrincipal.Identity, appPrincipal.Identity, devPrincipal.Identity });
            var session = sesService.Establish(sesPrincipal, "localhost", false, null, new string[] { "*" }, "en");
            Assert.IsNotNull(session.RefreshToken);
            Assert.IsNotNull(session.Id);
            Assert.IsTrue(session.Claims.Count(o => o.Type == SanteDBClaimTypes.SanteDBScopeClaim) > 2);

            // Extend
            Thread.Sleep(200); // Wait 1 seconds
            var extendedSession = sesService.Extend(session.RefreshToken);
            Assert.Greater(extendedSession.NotBefore, session.NotBefore);
            Assert.Greater(extendedSession.NotAfter, session.NotAfter);
            Assert.AreNotEqual(extendedSession.RefreshToken, session.RefreshToken);
            Assert.AreEqual(extendedSession.Id, session.Id);
        }

        /// <summary>
        /// Test that authentication on an abandoned sesison is not permitted
        /// </summary>
        [Test]
        public void TestAbandonSession()
        {
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var appService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            var devService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            var sesService = ApplicationServiceContext.Current.GetService<ISessionProviderService>();
            var sesIdService = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>();
            // Create all items
            identityService.CreateIdentity("TEST_USER_SES_04", "@P4SSW0r$", AuthenticationContext.SystemPrincipal);
            appService.CreateIdentity("TEST_APP_SES_04", "TEST_APP_SECRET", AuthenticationContext.SystemPrincipal);
            devService.CreateIdentity("TEST_DEV_SES_04", "TEST_DEV_SECRET", AuthenticationContext.SystemPrincipal);

            // Authentication
            var userPrincipal = identityService.Authenticate("TEST_USER_SES_04", "@P4SSW0r$");
            var appPrincipal = appService.Authenticate("TEST_APP_SES_04", "TEST_APP_SECRET");
            var devPrincipal = devService.Authenticate("TEST_DEV_SES_04", "TEST_DEV_SECRET");

            // Combo principal
            var tPrincipal = new SanteDBClaimsPrincipal(new IIdentity[] { userPrincipal.Identity, appPrincipal.Identity, devPrincipal.Identity });
            var session = sesService.Establish(tPrincipal, "localhost", false, null, new string[] { "*" }, "en");

            // Attempt to auth via session
            var sesPrincipal = sesIdService.Authenticate(session) as IClaimsPrincipal;
            Assert.AreEqual(userPrincipal.Identity.Name, sesPrincipal.Identity.Name);

            sesService.Abandon(session);

            try
            {
                sesIdService.Authenticate(session);
                Assert.Fail("Should not allow auth on abandoned session");
            }
            catch (AuthenticationException e) when (e.Message == this.m_localizationService.GetString(ErrorMessageStrings.SESSION_EXPIRE))
            {
            }
            catch (SecuritySessionException e) when (e.Type == SessionExceptionType.Expired)
            {
            }
            catch
            {
                Assert.Fail("Wrong exception type");
            }
        }

        /// <summary>
        /// Test the retrieval of a session by id
        /// </summary>
        [Test]
        public void TestGetSession()
        {
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var appService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            var devService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            var sesService = ApplicationServiceContext.Current.GetService<ISessionProviderService>();
            var sesIdService = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>();
            // Create all items
            identityService.CreateIdentity("TEST_USER_SES_05", "@P4SSW0r$", AuthenticationContext.SystemPrincipal);
            appService.CreateIdentity("TEST_APP_SES_05", "TEST_APP_SECRET", AuthenticationContext.SystemPrincipal);
            devService.CreateIdentity("TEST_DEV_SES_05", "TEST_DEV_SECRET", AuthenticationContext.SystemPrincipal);

            // Authentication
            var userPrincipal = identityService.Authenticate("TEST_USER_SES_05", "@P4SSW0r$");
            var appPrincipal = appService.Authenticate("TEST_APP_SES_05", "TEST_APP_SECRET");
            var devPrincipal = devService.Authenticate("TEST_DEV_SES_05", "TEST_DEV_SECRET");

            // Combo principal
            var tPrincipal = new SanteDBClaimsPrincipal(new IIdentity[] { userPrincipal.Identity, appPrincipal.Identity, devPrincipal.Identity });
            var session = sesService.Establish(tPrincipal, "localhost", false, null, new string[] { "*" }, "en");

            var rSession = sesService.Get(session.Id);
            Assert.AreEqual(session.Id, rSession.Id);
            Assert.IsNull(rSession.RefreshToken); // refresh not shown
        }

        /// <summary>
        /// Test the retrieval of a session identities by id
        /// </summary>
        [Test]
        public void TestGetSessionIdentity()
        {
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var appService = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            var devService = ApplicationServiceContext.Current.GetService<IDeviceIdentityProviderService>();
            var sesService = ApplicationServiceContext.Current.GetService<ISessionProviderService>();
            var sesIdService = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>();
            // Create all items
            identityService.CreateIdentity("TEST_USER_SES_06", "@P4SSW0r$", AuthenticationContext.SystemPrincipal);
            appService.CreateIdentity("TEST_APP_SES_06", "TEST_APP_SECRET", AuthenticationContext.SystemPrincipal);
            devService.CreateIdentity("TEST_DEV_SES_06", "TEST_DEV_SECRET", AuthenticationContext.SystemPrincipal);

            // Authentication
            var userPrincipal = identityService.Authenticate("TEST_USER_SES_06", "@P4SSW0r$");
            var appPrincipal = appService.Authenticate("TEST_APP_SES_06", "TEST_APP_SECRET");
            var devPrincipal = devService.Authenticate("TEST_DEV_SES_06", "TEST_DEV_SECRET");

            // Combo principal
            var tPrincipal = new SanteDBClaimsPrincipal(new IIdentity[] { userPrincipal.Identity, appPrincipal.Identity, devPrincipal.Identity });
            var session = sesService.Establish(tPrincipal, "localhost", false, null, new string[] { "*" }, "en");

            var identities = sesIdService.GetIdentities(session);
            Assert.AreEqual(3, identities.Length);
            Assert.IsTrue(identities.All(o => !o.IsAuthenticated));
        }
    }
}