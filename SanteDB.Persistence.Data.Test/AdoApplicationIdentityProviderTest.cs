﻿using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// Application identity provider test
    /// </summary>
    [TestFixture(Category = "Persistence", TestName = "ADO App Identity Provider")]
    public class AdoApplicationIdentityProviderTest : DataPersistenceTest
    {
       
        /// <summary>
        /// Verifies that a device entity can be persisted properly
        /// </summary>
        [Test]
        public void TestCreateApplicationIdentity()
        {

            var serviceProvider = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_APP_001"));

            // Create the device identity 
            var appIdentity = serviceProvider.CreateIdentity("TEST_APP_001", "THIS_IS_A_SECRET", AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(appIdentity);
            Assert.AreEqual("TEST_APP_001", appIdentity.Name);
            Assert.IsFalse(appIdentity.IsAuthenticated);

        }

        /// <summary>
        /// Tests that a regular principal cannot create 
        /// </summary>
        [Test]
        public void TestNonPrivCannotCreate()
        {
            var serviceProvider = ApplicationServiceContext.Current.GetService<IApplicationIdentityProviderService>();
            // Pre-Condition
            Assert.IsNull(serviceProvider.GetIdentity("TEST_APP_002"));

            // Create the device identity 
            try
            {
                var appIdentity = serviceProvider.CreateIdentity("TEST_APP_002", "THIS_IS_A_SECRET", AuthenticationContext.AnonymousPrincipal);
                Assert.Fail("Anonymous should not be able to create an application identity");
            }
            catch(PolicyViolationException e) {
                Assert.AreEqual(PermissionPolicyIdentifiers.CreateApplication, e.PolicyId);
                
            }
            catch(Exception)
            {
                Assert.Fail("Should throw a policy violation exception");
            }
        }
    }
}