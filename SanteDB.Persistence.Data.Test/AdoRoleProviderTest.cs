﻿using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// ADO Role Provider
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO Role Provider")]
    public class AdoRoleProviderTest : DataPersistenceTest
    {


        /// <summary>
        /// Can get all roles
        /// </summary>
        [Test]
        public void TestGetAllRoles()
        {
            var roleProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            var roles = roleProvider.GetAllRoles();
            Assert.IsTrue(roles.Contains("ADMINISTRATORS"));
            Assert.IsTrue(roles.Contains("USERS"));
            Assert.IsTrue(roles.Contains("CLINICAL_STAFF"));
        }

        /// <summary>
        /// Test creation of role
        /// </summary>
        [Test]
        public void TestCreateRole()
        {
            var roleProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            var roles = roleProvider.GetAllRoles();
            roleProvider.CreateRole("TEST_ROLE_01", AuthenticationContext.SystemPrincipal);
            Assert.Greater(roleProvider.GetAllRoles().Length, roles.Length);
        }

        /// <summary>
        /// Test adding user to roles
        /// </summary>
        [Test]
        public void AddUsersToRole()
        {
            var roleProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            roleProvider.CreateRole("TEST_ROLE_02", AuthenticationContext.SystemPrincipal);
            Assert.IsFalse(roleProvider.IsUserInRole("allison", "TEST_ROLE_02"));
            Assert.IsFalse(roleProvider.IsUserInRole("bob", "TEST_ROLE_02"));
            roleProvider.AddUsersToRoles(new string[] { "allison", "bob" }, new string[] { "TEST_ROLE_02" }, AuthenticationContext.SystemPrincipal);
            Assert.IsTrue(roleProvider.IsUserInRole("bob", "TEST_ROLE_02"));
            Assert.IsTrue(roleProvider.IsUserInRole("allison", "TEST_ROLE_02"));
        }

        /// <summary>
        /// Tests that the get roles for user works
        /// </summary>
        [Test]
        public void GetRolesForUser()
        {
            var roleProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            roleProvider.CreateRole("TEST_ROLE_03", AuthenticationContext.SystemPrincipal);
            var aroles = roleProvider.GetAllRoles("allison");
            Assert.Greater(aroles.Length, 0);
            roleProvider.AddUsersToRoles(new string[] { "allison" }, new string[] { "TEST_ROLE_03" }, AuthenticationContext.SystemPrincipal);
            Assert.Greater(roleProvider.GetAllRoles("allison").Length, aroles.Length);
            Assert.IsTrue(roleProvider.GetAllRoles("allison").Contains("TEST_ROLE_03"));
        }

        /// <summary>
        /// Test adding and removing user to roles
        /// </summary>
        [Test]
        public void RemoveUsersFromRole()
        {
            var roleProvider = ApplicationServiceContext.Current.GetService<IRoleProviderService>();
            roleProvider.CreateRole("TEST_ROLE_04", AuthenticationContext.SystemPrincipal);
            Assert.IsFalse(roleProvider.IsUserInRole("allison", "TEST_ROLE_04"));
            Assert.IsFalse(roleProvider.IsUserInRole("bob", "TEST_ROLE_04"));
            roleProvider.AddUsersToRoles(new string[] { "allison", "bob" }, new string[] { "TEST_ROLE_04" }, AuthenticationContext.SystemPrincipal);
            Assert.IsTrue(roleProvider.IsUserInRole("bob", "TEST_ROLE_04"));
            Assert.IsTrue(roleProvider.IsUserInRole("allison", "TEST_ROLE_04"));

            roleProvider.RemoveUsersFromRoles(new string[] { "allison", "bob" }, new string[] { "TEST_ROLE_04" }, AuthenticationContext.SystemPrincipal);
            Assert.IsFalse(roleProvider.IsUserInRole("allison", "TEST_ROLE_04"));
            Assert.IsFalse(roleProvider.IsUserInRole("bob", "TEST_ROLE_04"));
        }
    }
}
