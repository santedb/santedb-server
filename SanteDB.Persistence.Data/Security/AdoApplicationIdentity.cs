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
 * Date: 2022-9-7
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Persistence.Data.Exceptions;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace SanteDB.Persistence.Data.Security
{
    /// <summary>
    /// Represents an identity for ADO authenticated application records
    /// </summary>
    internal sealed class AdoApplicationIdentity : AdoIdentity, IApplicationIdentity, IClaimsIdentity
    {
        // Application
        private readonly DbSecurityApplication m_application;

        /// <summary>
        /// Create a new application identity
        /// </summary>
        internal AdoApplicationIdentity(DbSecurityApplication application, String authenticationMethod) : base(application.PublicId, authenticationMethod, true)
        {
            // Has the user been locked since the session was established?
            if (application.Lockout > DateTimeOffset.Now)
            {
                throw new LockedIdentityAuthenticationException(application.Lockout.Value);
            }
            else if (application.ObsoletionTime.HasValue)
            {
                throw new InvalidIdentityAuthenticationException();
            }

            this.m_application = application;
            this.m_application.Secret = null;
            this.m_application.PublicSigningKey = null;
            this.InitializeClaims();
        }

        /// <summary>
        /// Create a new un-authenticated application identity
        /// </summary>
        internal AdoApplicationIdentity(DbSecurityApplication application) : base(application.PublicId, null, false)
        {
            this.m_application = application;
            this.InitializeClaims();
        }

        /// <summary>
        /// Initialize claims
        /// </summary>
        private void InitializeClaims()
        {
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Sid, this.m_application.Key.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim, this.m_application.Key.ToString()));
            this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Actor, ActorTypeKeys.Application.ToString()));
        }

        /// <summary>
        /// Get the SID of this object
        /// </summary>
        internal override Guid Sid => this.m_application.Key;
    }
}