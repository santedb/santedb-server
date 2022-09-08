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
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Security challenge tests
    /// </summary>
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class AdoSecurityChallengeTest : DataPersistenceTest
    {

        /// <summary>
        /// Test that setting a security challenge works
        /// </summary>
        [Test]
        public void TestSetSecurityChallenge()
        {
            using(AuthenticationContext.EnterSystemContext())
            {
                var chlProvider = ApplicationServiceContext.Current.GetService<ISecurityChallengeService>();
                var chlAuthenticator = ApplicationServiceContext.Current.GetService<ISecurityChallengeIdentityService>();
                var chlRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<SecurityChallenge>>();
                var idProvider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
                var rpProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>();

                var userName = $"test-{Guid.NewGuid().ToString().Substring(0, 6)}";
                idProvider.CreateIdentity(userName, "@@TeST123", AuthenticationContext.SystemPrincipal);
                rpProvider.AddUsersToRoles(new string[] { userName }, new string[] { "USERS" }, AuthenticationContext.SystemPrincipal);
                Assert.IsNotNull(chlProvider);
                Assert.IsNotNull(chlAuthenticator);
                Assert.IsNotNull(chlRepository);

                // Validate we have challenges
                var challenges = chlRepository.Find(o => o.ObsoletionTime == null).Select(o=>o.Key);
                Assert.Greater(challenges.Count(), 0);

                // There are no challenges for administrator
                Assert.AreEqual(0,chlProvider.Get(userName, AuthenticationContext.AnonymousPrincipal).Count());
                
                // We should not be able to add a challenge as non-administrator
                try
                {
                    chlProvider.Set(userName, challenges.First().Value, "TEST", AuthenticationContext.AnonymousPrincipal);
                    Assert.Fail("Should have thrown a security exception!");
                }
                catch(SecurityException) { }
                catch
                {
                    Assert.Fail("Expected a SecurityException to be thrown");
                }

                // Now we can authenticate and set as a user
                var userPrincipal = idProvider.Authenticate(userName, "@@TeST123");
                chlProvider.Set(userName, challenges.First().Value, "TEST", userPrincipal);

                // Now make sure there is one returned as self and other
                Assert.AreEqual(1, chlProvider.Get(userName, userPrincipal).Count());
                Assert.AreEqual(1, chlProvider.Get(userName, AuthenticationContext.AnonymousPrincipal).Count());

                // Now set another secret challenge
                chlProvider.Set(userName, challenges.Skip(1).Take(1).First().Value, "TEST", userPrincipal);
                // Now make sure there is two returned as self and one as other
                Assert.AreEqual(2, chlProvider.Get(userName, userPrincipal).Count());
                Assert.AreEqual(1, chlProvider.Get(userName, AuthenticationContext.AnonymousPrincipal).Count());

                // Now attempt a removal
                try
                {
                    chlProvider.Remove(userName, challenges.First().Value, AuthenticationContext.AnonymousPrincipal);
                    Assert.Fail("Should have thrown exception");
                }
                catch(SecurityException)
                {

                }
                catch
                {
                    Assert.Fail("Threw wrong type of exception!");
                }

                // Removal
                chlProvider.Remove(userName, challenges.First().Value, userPrincipal);

                // Validate
                Assert.AreEqual(1, chlProvider.Get(userName, userPrincipal).Count());

                // Now attempt to authenticate
                var ap = chlAuthenticator.Authenticate(userName, chlProvider.Get(userName, userPrincipal).First().Key.Value, "TEST", null);


            }
        }

    }
}
