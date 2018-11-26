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
 * Date: 2018-9-25
 */
using System;
using System.Security.Principal;

namespace SanteDB.Core.Security.Claims
{
    /// <summary>
    /// Claim type handler
    /// </summary>
    public class OverrideClaimHandler : IClaimTypeHandler
    {
        /// <summary>
        /// Claim type handler for override
        /// </summary>
        public string ClaimType => SanteDBClaimTypes.SanteDBOverrideClaim;

        /// <summary>
        /// User wants to override their claims, is this allowed?
        /// </summary>
        public bool Validate(IPrincipal principal, string value)
        {
            Boolean b;
            return Boolean.TryParse(value, out b);
        }
    }
}
