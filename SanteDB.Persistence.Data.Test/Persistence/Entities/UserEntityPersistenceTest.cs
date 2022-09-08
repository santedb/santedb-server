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
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core.Services;

namespace SanteDB.Persistence.Data.Test.Persistence.Entities
{
    /// <summary>
    /// User entity persistence test
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class UserEntityPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Ensures that the persistence of a user entity with the proper persistence service
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var userService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
                userService.CreateIdentity("TEST_USER_ENTITY", "@Foo123!", AuthenticationContext.SystemPrincipal);
                var userEntity = new UserEntity()
                {
                    Addresses = new List<EntityAddress>()
                    {
                        new EntityAddress(AddressUseKeys.HomeAddress, "123 Main Street East", "Hamilton", "ON", "CA", "L8K5N2")
                    },
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Legal, "Test", "User")
                    },
                    LanguageCommunication = new List<PersonLanguageCommunication>()
                    {
                        new PersonLanguageCommunication("en", true)
                    },
                    SecurityUserKey = userService.GetSid("TEST_USER_ENTITY"),
                    DateOfBirth = new DateTime(1984, 05, 04)
                };

                // Insert user entity
                var afterInsert = base.TestInsert(userEntity);
                Assert.IsInstanceOf<UserEntity>(userEntity);
                Assert.IsNull(afterInsert.SecurityUser);
                Assert.AreEqual("TEST_USER_ENTITY", afterInsert.LoadProperty(o => o.SecurityUser).UserName);

                // Attempt via the repository service
                var securityService = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
                Assert.IsNotNull(securityService);

                var principal = userService.Authenticate("TEST_USER_ENTITY", "@Foo123!");

                var ue = securityService.GetUserEntity(principal.Identity);
                Assert.AreEqual(ue.Key, afterInsert.Key);

                // Attempt lookup by user name
                var afterQuery = base.TestQuery<UserEntity>(o => o.SecurityUser.UserName == "TEST_USER_ENTITY", 1).AsResultSet().First();
                Assert.AreEqual(afterQuery.Key, afterInsert.Key);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.LanguageCommunication).Count);

            }

        }

        /// <summary>
        /// Ensures that persistence of user entities wotks with an improper persistence (Entity)
        /// </summary>
        [Test]
        public void TestInsertWithImproperEntity()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var userService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
                userService.CreateIdentity("TEST_USER_ENTITY2", "@Foo123!", AuthenticationContext.SystemPrincipal);
                var userEntity = new UserEntity()
                {
                    Addresses = new List<EntityAddress>()
                    {
                        new EntityAddress(AddressUseKeys.HomeAddress, "123 Main Street East", "Hamilton", "ON", "CA", "L8K5N2")
                    },
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Legal, "Test", "User2")
                    },
                    LanguageCommunication = new List<PersonLanguageCommunication>()
                    {
                        new PersonLanguageCommunication("en", true)
                    },
                    SecurityUserKey = userService.GetSid("TEST_USER_ENTITY2"),
                    DateOfBirth = new DateTime(1984, 05, 04)
                };

                // Insert user entity
                var afterInsert = base.TestInsert<Entity>(userEntity);
                Assert.IsInstanceOf<UserEntity>(userEntity);
                Assert.AreEqual("TEST_USER_ENTITY2", (afterInsert as UserEntity).LoadProperty(o => o.SecurityUser).UserName);

                // Attempt via the repository service
                var securityService = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
                Assert.IsNotNull(securityService);

                var principal = userService.Authenticate("TEST_USER_ENTITY2", "@Foo123!");

                var ue = securityService.GetUserEntity(principal.Identity);
                Assert.AreEqual(ue.Key, afterInsert.Key);

                // Attempt lookup by user name
                var afterQuery = base.TestQuery<Entity>(o => o.Key == ue.Key, 1).AsResultSet().First();
                Assert.IsInstanceOf<UserEntity>(afterQuery);
                Assert.AreEqual(afterQuery.Key, afterInsert.Key);

            }
        }

        /// <summary>
        /// Ensures that persistence of user entities wotks with an improper persistence (Person)
        /// </summary>
        [Test]
        public void TestInsertWithImproperPerson()
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                var userService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
                userService.CreateIdentity("TEST_USER_ENTITY3", "@Foo123!", AuthenticationContext.SystemPrincipal);
                var userEntity = new UserEntity()
                {
                    Addresses = new List<EntityAddress>()
                    {
                        new EntityAddress(AddressUseKeys.HomeAddress, "123 Main Street East", "Hamilton", "ON", "CA", "L8K5N2")
                    },
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Legal, "Test", "User3")
                    },
                    LanguageCommunication = new List<PersonLanguageCommunication>()
                    {
                        new PersonLanguageCommunication("en", true)
                    },
                    SecurityUserKey = userService.GetSid("TEST_USER_ENTITY3"),
                    DateOfBirth = new DateTime(1984, 05, 04)
                };

                // Insert user entity
                var afterInsert = base.TestInsert<Person>(userEntity);
                Assert.IsInstanceOf<UserEntity>(userEntity);
                Assert.AreEqual("TEST_USER_ENTITY3", (afterInsert as UserEntity).LoadProperty(o => o.SecurityUser).UserName);

                // Attempt via the repository service
                var securityService = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
                Assert.IsNotNull(securityService);

                var principal = userService.Authenticate("TEST_USER_ENTITY3", "@Foo123!");

                var ue = securityService.GetUserEntity(principal.Identity);
                Assert.AreEqual(ue.Key, afterInsert.Key);

                // Attempt lookup by user name
                var afterQuery = base.TestQuery<Person>(o => o.Key == ue.Key, 1).AsResultSet().First();
                Assert.IsInstanceOf<UserEntity>(afterQuery);
                Assert.AreEqual(afterQuery.Key, afterInsert.Key);
                Assert.AreEqual(1, afterQuery.LoadProperty(o => o.LanguageCommunication).Count);

            }
        }
    }
}
