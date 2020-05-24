/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Http;
using System;
using System.Security.Principal;

namespace SanteDB.Tools.AdminConsole.Security
{
    /// <summary>
    /// Represents a Credential which is a token credential
    /// </summary>
    public class TokenCredentials : Credentials
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SanteDB.Tools.AdminConsole.Security.TokenCredentials"/> class.
		/// </summary>
		/// <param name="principal">Principal.</param>
		public TokenCredentials (IPrincipal principal) : base (principal)
		{
			
		}

		#region implemented abstract members of Credentials
		/// <summary>
		/// Get HTTP header
		/// </summary>
		/// <returns>The http headers.</returns>
		public override System.Collections.Generic.Dictionary<string, string> GetHttpHeaders ()
		{
            if (this.Principal is TokenClaimsPrincipal)
                return new System.Collections.Generic.Dictionary<string, string>() {
                    { "Authorization", String.Format ("Bearer {0}", this.Principal.ToString ()) }
                };
            else
                return new System.Collections.Generic.Dictionary<string, string>();
		}
		#endregion
	}


}

