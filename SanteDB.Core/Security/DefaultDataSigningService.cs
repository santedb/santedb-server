using SanteDB.Core.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Default data signature service
    /// </summary>
    public class DefaultDataSigningService : IDataSigningService
    {
        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string ServiceName => "Digital Signature Service";

        // Master configuration
        private SecurityConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        /// <summary>
        /// Create signing credentials
        /// </summary>
        private SigningCredentials CreateSigningCredentials()
        {
            SigningCredentials retVal = null;
            // Signing credentials
            if (this.m_configuration.Signatures.Algorithm == SignatureAlgorithm.RS256)
            {
                var cert = X509CertificateUtils.FindCertificate(this.m_configuration.Signatures.FindType, this.m_configuration.Signatures.StoreLocation, this.m_configuration.Signatures.StoreName, this.m_configuration.Signatures.FindValue);
                if (cert == null)
                    throw new SecurityException("Cannot find certificate to sign data!");
                retVal = new X509SigningCredentials(cert);
            }
            else if (this.m_configuration.Signatures.Algorithm == SignatureAlgorithm.HS256)
            {
                retVal = new SigningCredentials(
                    new InMemorySymmetricSecurityKey(this.m_configuration.Signatures.Secret),
                    "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256",
                    "http://www.w3.org/2001/04/xmlenc#sha256",
                    new SecurityKeyIdentifier(new NamedKeySecurityKeyIdentifierClause("keyid", "0"))
                );
            }
            else
                throw new SecurityException("Invalid signing configuration");
            return retVal;
        }

        /// <summary>
        /// Sign the specfiied data
        /// </summary>
        public byte[] SignData(byte[] data, int keyId = 0)
        {
            var credentials = this.CreateSigningCredentials();
            using (var signatureProvider = new SignatureProviderFactory().CreateForSigning(credentials.SigningKey, credentials.SignatureAlgorithm))
                return signatureProvider.Sign(data);
        }

        /// <summary>
        /// Verify the signature
        /// </summary>
        public bool Verify(byte[] data, byte[] signature, int keyId = 0)
        {
            var credentials = this.CreateSigningCredentials();
            using (var signatureProvider = new SignatureProviderFactory().CreateForVerifying(credentials.SigningKey, credentials.SignatureAlgorithm))
                return signatureProvider.Verify(data, signature);
        }
    }
}
