/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Core.Security
{
    /// <summary>
    /// Represents a symmetric cryptographic provider based on AES
    /// </summary>
    public class AesSymmetricCrypographicProvider : ISymmetricCryptographicProvider
    {

        // Context key
        private byte[] m_contextKey;

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
                var retVal = encryptor.TransformFinalBlock(data, 0, data.Length);
                return retVal;
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
            if (this.m_contextKey == null)
            {
                // TODO: Actually handle RSA data
                var defaultKey = this.m_configuration.Signatures.FirstOrDefault(o => String.IsNullOrEmpty(o.KeyName) || o.KeyName == "default");
                this.m_contextKey = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(defaultKey?.FindValue ?? defaultKey?.HmacSecret ?? "DEFAULTKEY"));
            }
            return this.m_contextKey;
        }
    }
}
