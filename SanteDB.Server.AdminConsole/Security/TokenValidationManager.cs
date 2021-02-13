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
using System;

namespace SanteDB.Server.AdminConsole.Security
{

    /// <summary>
    /// Indicates whether a remote symmetric key validation error should be ignored
    /// </summary>
    public delegate bool SymmetricKeyValidationCallback(Object source, int keyId, String issuer);

    /// <summary>
    /// Symmetric key validation manager
    /// </summary>
    public static class TokenValidationManager
    {

        /// <summary>
        /// Symmtric key validation callback
        /// </summary>
        public static event SymmetricKeyValidationCallback SymmetricKeyValidationCallback;

        /// <summary>
        /// Server URI
        /// </summary>
        internal static bool OnSymmetricKeyValidationCallback(Object source, int keyId, String issuer)
        {
            return SymmetricKeyValidationCallback?.Invoke(source, keyId, issuer) == true;
        } 

    }
}
