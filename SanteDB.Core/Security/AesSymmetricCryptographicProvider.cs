using SanteDB.Core.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Represents a symmetric cryptographic provider based on AES
    /// </summary>
    public class AesSymmetricCrypographicProvider : ISymmetricCryptographicProvider
    {

        // Configuration
        private SecurityConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        /// <summary>
        /// Service name
        /// </summary>
        public String ServiceName => "AES Symmetric Cryptographic Provider";

        /// <summary>
        /// Decrypt the specified data using the specified key and iv
        /// </summary>
        public byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            key = this.CorrectKey(key);
            using (var aes = new AesCryptoServiceProvider())
            {
                var decryptor = aes.CreateDecryptor(key, iv);
                byte[] outputBuffer = new byte[1024];
                return decryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Ensure the key is the right length
        /// </summary>
        private byte[] CorrectKey(byte[] key)
        {
            byte[] retVal = new byte[key.Length * 3];
            for (int i = 0; i < 32; i += key.Length)
                Array.Copy(key, 0, retVal, i, key.Length);
            return retVal.Take(32).ToArray();
        }

        /// <summary>
        /// Encrypt the specified data using the specified key and iv
        /// </summary>
        public byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            key = this.CorrectKey(key);
            using (var aes = new AesCryptoServiceProvider())
            {
                var encryptor = aes.CreateEncryptor(key, iv);
                byte[] outputBuffer = new byte[1024];
                return encryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Generate a random IV
        /// </summary>
        public byte[] GenerateIV()
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.GenerateIV();
                return aes.IV;
            }
        }

        /// <summary>
        /// Generate key
        /// </summary>
        /// <returns></returns>
        public byte[] GenerateKey()
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.GenerateKey();
                return aes.Key;
            }
        }

        /// <summary>
        /// Get the context default key
        /// </summary>
        public byte[] GetContextKey()
        {
            // TODO: Is it possible to pull from CPU?
            var defaultKey = this.m_configuration.Signatures.FirstOrDefault(o => String.IsNullOrEmpty(o.KeyName) || o.KeyName == "default");
            return SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(defaultKey?.HmacSecret ?? "DEFAULTKEY"));
        }
    }
}
