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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Principal;

namespace SanteDB.Server.Core.Security
{
    /// <summary>
    /// Represents a default implementation of a TFA relay service which scans the entire application domain for 
    /// mechanisms and allows calling of them all
    /// </summary>
    [ServiceProvider("Default TFA Relay Provider")]
    public class DefaultTfaRelayService : ITfaRelayService
    {

        private Tracer m_tracer = Tracer.GetTracer(typeof(DefaultTfaRelayService));

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Default TFA Relay Provider";

        /// <summary>
        /// Construct the default relay service
        /// </summary>
        public DefaultTfaRelayService(IServiceManager serviceManager)
        {
            this.Mechanisms = serviceManager.CreateInjectedOfAll<ITfaMechanism>();
        }

        /// <summary>
        /// Gets the configured mechanisms
        /// </summary>
        public IEnumerable<ITfaMechanism> Mechanisms
        {
            get; private set;
        }

        /// <summary>
        /// Sends the secret via the specified mechanism
        /// </summary>
        public String SendSecret(Guid mechanismId, IIdentity user)
        {
            // Get the mechanism
            var mechanism = this.Mechanisms.FirstOrDefault(o => o.Id == mechanismId);
            if (mechanism == null)
                throw new SecurityException($"TFA mechanism {mechanismId} not found");

            // send the secret
            return mechanism.Send(user);
        }

        /// <summary>
        /// Validate the secret
        /// </summary>
        public bool ValidateSecret(Guid mechanismId, IIdentity user, String secret)
        {
            // Get the mechanism
            var mechanism = this.Mechanisms.FirstOrDefault(o => o.Id == mechanismId);
            if (mechanism == null)
                throw new SecurityException($"TFA mechanism {mechanismId} not found");

            // send the secret
            return mechanism.Validate(user, secret);
        }
    }
}
