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
        /// Sign the specfiied data
        /// </summary>
        public byte[] SignData(byte[] data, string keyId = null)
        {
            var credentials = SecurityUtils.CreateSigningCredentials(keyId);
            using (var signatureProvider = new SignatureProviderFactory().CreateForSigning(credentials.SigningKey, credentials.SignatureAlgorithm))
                return signatureProvider.Sign(data);
        }

        /// <summary>
        /// Verify the signature
        /// </summary>
        public bool Verify(byte[] data, byte[] signature, string keyId = null)
        {
            var credentials = SecurityUtils.CreateSigningCredentials(keyId);
            using (var signatureProvider = new SignatureProviderFactory().CreateForVerifying(credentials.SigningKey, credentials.SignatureAlgorithm))
                return signatureProvider.Verify(data, signature);
        }

        /// <summary>
        /// Get the specified signature algorithm
        /// </summary>
        public string GetSignatureAlgorithm(string keyId = null)
        {
            return SecurityUtils.CreateSigningCredentials(keyId)?.SignatureAlgorithm;
        }
    }
}
