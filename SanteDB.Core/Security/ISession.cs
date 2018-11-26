/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-6-22
 */
using System;

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Represents session information related to a user
    /// </summary>
    public interface ISession 
    {

        /// <summary>
        /// Gets the identifier of the session
        /// </summary>
        byte[] Id { get; }

        /// <summary>
        /// Gets the time the session was established
        /// </summary>
        DateTimeOffset NotBefore { get; }

        /// <summary>
        /// Gets the time the session expires
        /// </summary>
        DateTimeOffset NotAfter { get; }

        /// <summary>
        /// Gets the refresh token
        /// </summary>
        byte[] RefreshToken { get; }

    }

    /// <summary>
    /// Represents a generic session
    /// </summary>
    public class GenericSession : ISession
    {
        /// <summary>
        /// Creates a generic session for the user
        /// </summary>
        /// <param name="id">The token identifier for the session</param>
        /// <param name="refreshToken">The token which can be used to extend the session</param>
        /// <param name="notBefore">Indicates a not-before time</param>
        /// <param name="notAfter">Indicates a not-after time</param>
        public GenericSession(byte[] id, byte[] refreshToken, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            this.Id = id;
            this.RefreshToken = refreshToken;
            this.NotBefore = notBefore;
            this.NotAfter = notAfter;
        }
        /// <summary>
        /// Gets the unique token identifier for the session
        /// </summary>
        public byte[] Id { get; private set; }

        /// <summary>
        /// Gets the issuance time
        /// </summary>
        public DateTimeOffset NotAfter { get; private set; }

        /// <summary>
        /// Get the expiration time
        /// </summary>
        public DateTimeOffset NotBefore { get; private set; }

        /// <summary>
        /// Gets the refresh token
        /// </summary>
        public byte[] RefreshToken { get; private set; }

    }
}
