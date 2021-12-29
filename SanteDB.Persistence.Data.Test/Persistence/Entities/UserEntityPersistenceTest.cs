using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
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
    public class UserEntityPersistenceTest : DataPersistenceTest
    {

        /// <summary>
        /// Ensures that the persistence of a user entity with the proper persistence service
        /// </summary>
        [Test]
        public void TestInsertWithProper()
        {
            using(AuthenticationContext.EnterSystemContext())
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
            }
        }
    }
}
