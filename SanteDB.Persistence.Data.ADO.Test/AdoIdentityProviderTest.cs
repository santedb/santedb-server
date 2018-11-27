using Microsoft.VisualStudio.TestTools.UnitTesting;
using SanteDB.Core;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System.Security.Authentication;

namespace SanteDB.Persistence.Data.ADO.Test
{
    [TestClass]
    public class AdoIdentityProviderTest : DataTest
    {

        /// <summary>
        /// Class startup
        /// </summary>
        /// <param name="context"></param>
        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {


            DataTestUtil.Start(context);
            IPasswordHashingService hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            var dataService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            if (dataService.GetIdentity("admin@identitytest.com") == null)
                dataService.CreateIdentity("admin@identitytest.com",  "password", AuthenticationContext.Current.Principal);
            if (dataService.GetIdentity("user@identitytest.com") == null)
                dataService.CreateIdentity("user@identitytest.com",  "password", AuthenticationContext.Current.Principal);

            IRoleProviderService roleService = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            roleService.AddUsersToRoles(new string[] { "admin@identitytest.com", "user@identitytest.com" },  new string[] { "USERS" },  AuthenticationContext.Current.Principal);
            roleService.AddUsersToRoles(new string[] { "admin@identitytest.com" },  new string[] { "ADMINISTRATORS" }, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Tests that the authenticate method successfully authenticates a user
        /// </summary>
        [TestMethod]
        public void TestAuthenticateSuccess()
        {

            IIdentityProviderService provider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var principal = provider.Authenticate("admin@identitytest.com", "password");
            Assert.AreEqual("admin@identitytest.com", principal.Identity.Name);
            Assert.IsTrue(principal.Identity.IsAuthenticated);
            Assert.AreEqual("Password", principal.Identity.AuthenticationType);

        }

        /// <summary>
        /// Tests that the authenticate method successfully retrieves a non-authenticated identity
        /// </summary>
        [TestMethod]
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
        [TestMethod]
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
        [TestMethod]
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
        [TestMethod]
        public void TestAuthenticateFailure()
        {
            try
            {
                IIdentityProviderService provider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
                var principal = provider.Authenticate("admin@identitytest.com", "passwordz");
                Assert.Fail("Should throw SecurityException");
            }
            catch(AuthenticationException)
            { }
        }

        /// <summary>
        /// Tests that the identity provider can change passwords
        /// </summary>
        [TestMethod]
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
        [TestMethod]
        public void TestAnonymousUserCreation()
        {

            var dataPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityUser>>();
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

            var identity = identityService.CreateIdentity("anonymous@identitytest.com",  "mypassword", AuthenticationContext.Current.Principal);
            Assert.IsNotNull(identity);
            Assert.IsFalse(identity.IsAuthenticated);

            // Now verify with data persistence
            //var dataUser = dataPersistence.Query(u => u.UserName == "anonymous@identitytest.com", null).First();
            //Assert.AreEqual(hashingService.ComputeHash("mypassword"), dataUser.PasswordHash);
        }

        /// <summary>
        /// Tests that administrative user creation works
        /// </summary>
        [TestMethod]
        public void TestAdministrativeUserCreation()
        {

           // var dataPersistence = ApplicationServiceContext.Current.GetService<IDataPersistenceService<SecurityUser>>();
            var identityService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var hashingService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

            var authContext = identityService.Authenticate("admin@identitytest.com", "password");
            AuthenticationContext.Current = new AuthenticationContext(authContext);
            var identity = identityService.CreateIdentity("admincreated@identitytest.com",  "mypassword", AuthenticationContext.Current.Principal);
            AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(identity);
            Assert.IsFalse(identity.IsAuthenticated);

            // Now verify with data persistence
            //var dataUser = dataPersistence.Query(u => u.UserName == "admincreated@identitytest.com", null).First();
            //Assert.AreEqual(hashingService.ComputeHash("mypassword"), dataUser.PasswordHash);
            //Assert.IsFalse(dataUser.Lockout.HasValue);
            //Assert.AreEqual(authContext.Identity.Name, dataUser.CreatedBy.UserName);
            

        }
    }
}
