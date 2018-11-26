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
using SanteDB.Core.Security;
using System;
using System.Security.Claims;

namespace SanteDB.Core.Services
{

    /// <summary>
    /// Represents a service which is responsible for the storage and retrieval of sessions
    /// </summary>
    public interface ISessionProviderService
    {

        /// <summary>
        /// Establishes a session for the specified principal
        /// </summary>
        /// <param name="principal">The principal for which the session is to be established</param>
        /// <param name="expiry">The time when the session is to expire</param>
        /// <param name="aud">The audience of the session</param>
        /// <returns>The session information that was established</returns>
        ISession Establish(ClaimsPrincipal principal, DateTimeOffset expiry, String aud);

        /// <summary>
        /// Authenticates the session identifier as evidence of session
        /// </summary>
        /// <param name="sessionToken">The session identiifer to be authenticated</param>
        /// <returns>The authenticated session from the session provider</returns>
        ISession Get(byte[] sessionToken);

        /// <summary>
        /// Extend the session with the specified refresh token
        /// </summary>
        /// <param name="refreshToken">The refresh token that will extend the session</param>
        /// <returns>The extended session</returns>
        ISession Extend(byte[] refreshToken);
    }
}
