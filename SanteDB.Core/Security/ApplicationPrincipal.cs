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
using SanteDB.Core.Security.Claims;
using System;

using System.Security.Principal;

namespace SanteDB.Core.Security
{

    /// <summary>
    /// Application identity
    /// </summary>
    public class ApplicationIdentity : SanteDBClaimsIdentity
    {
        // Member variables
        private string m_name;
        private bool m_isAuthenticated;

        /// <summary>
        /// Application identity ctor
        /// </summary>
        public ApplicationIdentity(Guid sid, String name, Boolean isAuthenticated) 
            : base(name, isAuthenticated, "SYSTEM")
        {
            this.m_name = name.ToString();
            this.m_isAuthenticated = isAuthenticated;
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Sid, sid.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim, sid.ToString()));
        }
        
    }
    /// <summary>
    /// Represents an IPrincipal related to an application
    /// </summary>
    public class ApplicationPrincipal : SanteDBClaimsPrincipal
    {

        /// <summary>
        /// Application principal
        /// </summary>
        public ApplicationPrincipal(IIdentity identity) : base(identity)
        {
        }

    }
}
