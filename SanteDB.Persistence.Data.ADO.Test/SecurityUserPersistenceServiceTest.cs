using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core.Model.Security;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Model.Constants;

namespace SanteDB.Persistence.Data.ADO.Test
{
    /// <summary>
    /// Summary description for SecurityUserPersistenceServiceTest
    /// </summary>
    [TestClass]
    public class SecurityUserPersistenceServiceTest : PersistenceTest<SecurityUser>
    {

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            DataTestUtil.Start(context);
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);

        }

        /// <summary>
        /// Test the insertion of a valid security user
        /// </summary>
        [TestMethod]
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
        [TestMethod]
        public void TestUpdateValidSecurityUser()
        {

            IPasswordHashingService hashingService = ApplicationContext.Current.GetService<IPasswordHashingService>();

            SecurityUser userUnderTest = new SecurityUser()
            {
                Email = "update@test.com",
                EmailConfirmed = false,
                Password = hashingService.EncodePassword("password"),
                SecurityHash = "cert",
                UserName = "updateTest",
                UserClass = UserClassKeys.HumanUser

            };
            
            // Store user
            IIdentityProviderService identityService = ApplicationContext.Current.GetService<IIdentityProviderService>();
            var authContext = AuthenticationContext.SystemPrincipal;
            Assert.IsNotNull(authContext);
            var userAfterUpdate = base.DoTestUpdate(userUnderTest, authContext, "PhoneNumber");

            // Update
            Assert.IsNotNull(userAfterUpdate.UpdatedTime);
            Assert.IsNotNull(userAfterUpdate.PhoneNumber);
            Assert.AreEqual(authContext.Identity.Name, userAfterUpdate.UpdatedBy.UserName);
        }

        /// <summary>
        /// Test valid query result
        /// </summary>
        [TestMethod]
        public void TestQueryValidResult()
        {

            IPasswordHashingService hashingService = ApplicationContext.Current.GetService<IPasswordHashingService>();
            String securityHash = Guid.NewGuid().ToString();
            SecurityUser userUnderTest = new SecurityUser()
            {
                Email = "query@test.com",
                EmailConfirmed = false,
                Password = hashingService.EncodePassword("password"),
                SecurityHash = securityHash,
                UserName = "queryTest",
                UserClass = UserClassKeys.HumanUser

            };

            var testUser = base.DoTestInsert(userUnderTest);
            IIdentityProviderService identityService = ApplicationContext.Current.GetService<IIdentityProviderService>();
            var results = base.DoTestQuery(o => o.Email == "query@test.com", testUser.Key, AuthenticationContext.SystemPrincipal);
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual(userUnderTest.Email, results.First().Email);
        }

        /// <summary>
        /// Tests the delay loading of properties works
        /// </summary>
        [TestMethod]
        public void TestDelayLoadUserProperties()
        {
            IPasswordHashingService hashingService = ApplicationContext.Current.GetService<IPasswordHashingService>();
            String securityHash = Guid.NewGuid().ToString();
            SecurityUser userUnderTest = new SecurityUser()
            {
                Email = "query@test.com",
                EmailConfirmed = false,
                Password = hashingService.EncodePassword("password"),
                SecurityHash = securityHash,
                UserName = "delayLoadTest",
                UserClass = UserClassKeys.HumanUser
            };


            var userAfterInsert = base.DoTestInsert(userUnderTest, null);
            var roleProvider = ApplicationContext.Current.GetService<IRoleProviderService>();
            var identityProvider = ApplicationContext.Current.GetService<IIdentityProviderService>();

            // Allow login
            roleProvider.AddUsersToRoles(new string[] { "delayLoadTest" }, new string[] { "USERS" }, AuthenticationContext.SystemPrincipal);

            var auth = identityProvider.Authenticate("delayLoadTest", "password");
            roleProvider.CreateRole("TestDelayLoadUserPropertiesGroup", AuthenticationContext.SystemPrincipal);
            roleProvider.AddUsersToRoles(new String[] { "delayLoadTest" }, new String[] { "TestDelayLoadUserPropertiesGroup" }, AuthenticationContext.SystemPrincipal);

            // Now trigger a delay load
            var userForTest = base.DoTestQuery(u => u.UserName == "delayLoadTest", userAfterInsert.Key, auth).First();
            Assert.AreEqual(2, userForTest.Roles.Count);
            Assert.IsTrue(userForTest.Roles.Exists(o=>o.Name == "TestDelayLoadUserPropertiesGroup"));


        }

    }
}
