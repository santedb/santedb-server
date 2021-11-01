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
    [Obsolete("Use SanteDB.Core.Security.SHA256PasswordHashingService")]
    public class SHA256PasswordHashingService : SanteDB.Core.Security.SHA256PasswordHashingService
    {
    }

    /// <summary>
    /// SHA1 password generator service
    /// </summary>
    [ServiceProvider("SHA1 Password Encoding Service")]
    [Obsolete("Use SanteDB.Core.Security.SHA1PasswordHashingService")]
    public class SHA1PasswordHashingService : SanteDB.Core.Security.SHA1PasswordHashingService
    {
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
            MD5 hasher = MD5.Create();
            return BitConverter.ToString(hasher.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-", "").ToLower();
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