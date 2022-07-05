using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// ADO.NET Certificate identity mapping provider test
    /// </summary>
    [TestFixture(Category = "Persistence")]
    [ExcludeFromCodeCoverage]
    public class AdoCertificateIdentityProviderTest : DataPersistenceTest
    {

        private X509Certificate2 GetCertificate()
        {
            return new X509Certificate2(X509Certificate2.CreateFromCertFile(
                Path.Combine(Path.GetDirectoryName(typeof(AdoCertificateIdentityProviderTest).Assembly.Location), 
                "test.lumon.com.cer")));
        }

        /// <summary>
        /// Tests that an asisgnment of a certificate from an identity is successful
        /// </summary>
        [Test]
        public void TestAssignCertificateToIdentity()
        {
            var certService = ApplicationServiceContext.Current.GetService<ICertificateIdentityProvider>();
            Assert.IsNotNull(certService);
            var userService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNotNull(userService);

            // Create a random user
            var userIdentity = userService.CreateIdentity("TEST_ADO_X509_01", "@TeST123!", AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(userIdentity);

            // Assign the certificate to the identity
            certService.AddIdentityMap(userIdentity, this.GetCertificate(), AuthenticationContext.SystemPrincipal);

            // Lookup 
            var cert = certService.GetIdentityCertificate(userIdentity);
            Assert.IsNotNull(cert);
            Assert.AreEqual(this.GetCertificate().Thumbprint, cert.Thumbprint);
            certService.RemoveIdentityMap(userIdentity, cert, AuthenticationContext.SystemPrincipal);

        }

        /// <summary>
        /// Test that a removal of a certificate from an identity is successful
        /// </summary>
        [Test]
        public void TestRemoveCertificateFromIdentity()
        {
            var certService = ApplicationServiceContext.Current.GetService<ICertificateIdentityProvider>();
            Assert.IsNotNull(certService);
            var userService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNotNull(userService);

            // Create a random user
            var userIdentity = userService.CreateIdentity("TEST_ADO_X509_02", "@TeST123!", AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(userIdentity);

            // Assign the certificate to the identity
            certService.AddIdentityMap(userIdentity, this.GetCertificate(), AuthenticationContext.SystemPrincipal);

            // Lookup 
            var cert = certService.GetIdentityCertificate(userIdentity);
            Assert.IsNotNull(cert);
            Assert.AreEqual(this.GetCertificate().Thumbprint, cert.Thumbprint);

            certService.RemoveIdentityMap(userIdentity, cert, AuthenticationContext.SystemPrincipal);
            try
            {
                cert = certService.GetIdentityCertificate(userIdentity);
                Assert.Fail("Should have thrown exception");
            }
            catch (Exception e) when (e.InnerException is KeyNotFoundException)
            {

            }
            catch
            {
                Assert.Fail("wrong exception type");
            }

        }


        /// <summary>
        /// Test lookup of identity from certificate
        /// </summary>
        [Test]
        public void TestLookupIdentityFromCertificate()
        {
            var certService = ApplicationServiceContext.Current.GetService<ICertificateIdentityProvider>();
            Assert.IsNotNull(certService);
            var userService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNotNull(userService);

            // Create a random user
            var userIdentity = userService.CreateIdentity("TEST_ADO_X509_03", "@TeST123!", AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(userIdentity);

            // Assign the certificate to the identity
            certService.AddIdentityMap(userIdentity, this.GetCertificate(), AuthenticationContext.SystemPrincipal);

            // Lookup 
            var id = certService.GetCertificateIdentity(this.GetCertificate());
            Assert.IsNotNull(id);
            Assert.AreEqual("TEST_ADO_X509_02", id.Name);

            certService.RemoveIdentityMap(userIdentity, this.GetCertificate(), AuthenticationContext.SystemPrincipal);
        }

        /// <summary>
        /// Test authentication with a certificate
        /// </summary>
        [Test]
        public void TestAuthenticateWithCertificate()
        {
            var certService = ApplicationServiceContext.Current.GetService<ICertificateIdentityProvider>();
            Assert.IsNotNull(certService);
            var userService = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            Assert.IsNotNull(userService);

            // Create a random user
            var userIdentity = userService.CreateIdentity("TEST_ADO_X509_04", "@TeST123!", AuthenticationContext.SystemPrincipal);
            Assert.IsNotNull(userIdentity);

            // Assign the certificate to the identity
            certService.AddIdentityMap(userIdentity, this.GetCertificate(), AuthenticationContext.SystemPrincipal);

            // Authenticate with a certificate
            var auth = certService.Authenticate(this.GetCertificate());
            Assert.AreEqual(userIdentity.Name, auth.Identity.Name);

            // Remove certificate 
            certService.RemoveIdentityMap(userIdentity, this.GetCertificate(), AuthenticationContext.SystemPrincipal);

            // Authenticate should fail
            try
            {
                certService.Authenticate(this.GetCertificate());
                Assert.Fail("Shoudl have thrown exception");
            }
            catch(AuthenticationException)
            {

            }
            catch(Exception e) when (e.InnerException is AuthenticationException)
            {

            }
            catch
            {
                Assert.Fail("Wrong exception type!");
            }
        }
    }
}
