/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SanteDB.Server.Core.Security
{
    /// <summary>
    /// SHA256 password generator service
    /// </summary>
    [ServiceProvider("SHA256 Password Encoding Service")]
    public class SHA256PasswordHashingService : IPasswordHashingService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "SHA256 Password Encoding Service";

        /// <summary>
        /// Encode a password using the SHA256 encoding
        /// </summary>
        public string ComputeHash(string password)
        {
            if(String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }
            SHA256 hasher = SHA256.Create();
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-","").ToLower();
        }
    }

    /// <summary>
    /// SHA1 password generator service
    /// </summary>
    [ServiceProvider("SHA1 Password Encoding Service")]
    public class SHA1PasswordHashingService : IPasswordHashingService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "SHA1 Password Encoding Service";

        /// <summary>
        /// Encode a password using the SHA256 encoding
        /// </summary>
        public string ComputeHash(string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            SHA1 hasher = SHA1.Create();
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-","").ToLower();
        }
    }

    /// <summary>
    /// SHA1 password generator service
    /// </summary>
    [ServiceProvider("MD5 Password Encoding Service")]
    public class MD5PasswordHashingService : IPasswordHashingService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "MD5 Password Encoding Service";

        /// <summary>
        /// Encode a password using the SHA256 encoding
        /// </summary>
        public string ComputeHash(string password)
        {
            if (String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            MD5 hasher = MD5.Create();
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-","").ToLower();
        }
    }

    /// <summary>
    /// Plaintext password generator service
    /// </summary>
    [ServiceProvider("Plaintext Password Encode")]
    public class PlainPasswordHashingService : IPasswordHashingService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public String ServiceName => "Plaintext Password Encoding Service";

        /// <summary>
        /// Encode a password using the SHA256 encoding
        /// </summary>
        public string ComputeHash(string password)
        {
            return password;
        }
    }
}
