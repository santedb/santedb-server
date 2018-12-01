using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Persistence.ADO.Test.Core;
using System;
using System.Linq;

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
            TestApplicationContext.TestAssembly = typeof(AdoIdentityProviderTest).Assembly;
            TestApplicationContext.Initialize(context.DeploymentDirectory);

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
            Assert.AreEqual(authContext.Identity.Name, userAfterUpdate.UpdatedBy.User.UserName);
        }

        /// <summary>
        /// Test valid query result
        /// </summary>
        [TestMethod]
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
        [TestMethod]
        public void TestDelayLoadUserProperties()
        {
            IPasswordHashingService hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
            String securityHash = Guid.NewGuid().ToString();
            SecurityUser userUnderTest = new SecurityUser()
            {
                Email = "query@test.com",
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

            var auth = identityProvider.Authenticate("delayLoadTest", "password");
            roleProvider.CreateRole("TestDelayLoadUserPropertiesGroup", auth);
            roleProvider.AddUsersToRoles(new String[] { "delayLoadTest" },  new String[] { "TestDelayLoadUserPropertiesGroup" }, AuthenticationContext.Current.Principal);

            // Now trigger a delay load
            var userForTest = base.DoTestQuery(u => u.UserName == "delayLoadTest", userAfterInsert.Key, auth).First();
            Assert.AreEqual(2, userForTest.Roles.Count);
            Assert.IsTrue(userForTest.Roles.Exists(o=>o.Name == "TestDelayLoadUserPropertiesGroup"));


        }

    }
}
