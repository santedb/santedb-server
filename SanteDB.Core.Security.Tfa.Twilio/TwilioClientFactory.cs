/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Core.Security.Tfa.Twilio.Configuration;
using SanteDB.Core.Services;
using Twilio.Clients;

namespace SanteDB.Security.Tfa.Twilio
{
    /// <summary>
    /// A factor which creates a Twilio client
    /// </summary>
    public class TwilioClientFactory : IServiceImplementation
    {
        /// <inheritdoc/>
        public string ServiceName => "Twilio Client Factory";

        readonly TwilioTfaMechanismConfigurationSection _Configuration;

        /// <summary>
        /// DI constructor
        /// </summary>
        public TwilioClientFactory(IConfigurationManager configurationManager)
        {
            _Configuration = configurationManager.GetSection<TwilioTfaMechanismConfigurationSection>();
        }

        /// <inheritdoc/>
        public ITwilioRestClient GetRestClient()
        {
            return new TwilioRestClient(_Configuration.Sid, _Configuration.Auth);
        }

        /// <inheritdoc/>
        public string FromNumber => _Configuration.From;
    }
}
