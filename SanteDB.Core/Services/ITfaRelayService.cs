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
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;

namespace SanteDB.Core.Services
{

    /// <summary>
    /// Represents the TFA mechanism
    /// </summary>
    public interface ITfaMechanism
    {

        /// <summary>
        /// Gets the identifier of the mechanism
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the name of the TFA mechanism
        /// </summary>
        String Name { get; }

        /// <summary>
        /// The description of this challenge mechanism
        /// </summary>
        String Description { get; }
        
        /// <summary>
        /// Send the specified two factor authentication via the mechanism 
        /// </summary>
        void Send(SecurityUser user, String challengeResponse, String tfaSecret);

    }

    /// <summary>
    /// Represents a two-factor authentication relay service
    /// </summary>
    public interface ITfaRelayService : IServiceImplementation
    {

        /// <summary>
        /// Send the secret for the specified user
        /// </summary>
        void SendSecret(Guid mechanismId, SecurityUser user, String mechanismVerification, String tfaSecret);
        
        /// <summary>
        /// Gets the tfa mechanisms supported by this relay service
        /// </summary>
        IEnumerable<ITfaMechanism> Mechanisms { get; }
        
    }
}
