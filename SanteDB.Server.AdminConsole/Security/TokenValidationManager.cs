/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using System;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Server.AdminConsole.Security
{

    /// <summary>
    /// Indicates whether a remote symmetric key validation error should be ignored
    /// </summary>
    public delegate bool SymmetricKeyValidationCallback(Object source, int keyId, String issuer);

    /// <summary>
    /// Symmetric key validation manager
    /// </summary>
    [ExcludeFromCodeCoverage]
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
