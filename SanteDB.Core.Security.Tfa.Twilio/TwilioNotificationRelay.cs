/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Notifications;
using System;
using System.Collections.Generic;
using Twlo = global::Twilio;

namespace SanteDB.Security.Tfa.Twilio
{
    /// <summary>
    /// An implementation of the <see cref="INotificationRelay"/>
    /// </summary>
    public class TwilioNotificationRelay : INotificationRelay
    {
        readonly TwilioClientFactory _ClientFactory;
        readonly Tracer _Tracer;

        /// <summary>
        /// DI constructor
        /// </summary>
        public TwilioNotificationRelay(TwilioClientFactory factory)
        {
            _Tracer = new Tracer(nameof(TwilioNotificationRelay));
            _ClientFactory = factory;
        }

        /// <inheritdoc/>
        public IEnumerable<string> SupportedSchemes => new[] { "sms", "tel" };

        /// <inheritdoc/>
        public Guid Send(string[] toAddress, string subject, string body, DateTimeOffset? scheduleDelivery = null, bool ccAdmins = false, params NotificationAttachment[] attachments)
        {
            if (null == toAddress)
            {
                throw new ArgumentNullException(nameof(toAddress));
            }

            var client = _ClientFactory.GetRestClient();

            //TODO: Gather each resource SID into a notification object and return the GUID for that object.

            foreach (var address in toAddress)
            {
                try
                {
                    var number = new EDot164Number(address);

                    if ("tel".Equals(number.Protocol, StringComparison.OrdinalIgnoreCase))
                    {
                        var call = Twlo.Rest.Api.V2010.Account.CallResource.Create(new Twlo.Rest.Api.V2010.Account.CreateCallOptions((Twlo.Types.PhoneNumber)number.Number, (Twlo.Types.PhoneNumber)_ClientFactory.FromNumber)
                        {
                            Twiml = body
                        }, client);

                        _Tracer.TraceVerbose("Outbound call request made: {0}", call?.Sid);
                    }
                    else //Assume SMS by default
                    {
                        var message = Twlo.Rest.Api.V2010.Account.MessageResource.Create(new Twlo.Rest.Api.V2010.Account.CreateMessageOptions(number.Number)
                        {
                            From = _ClientFactory.FromNumber,
                            Body = body
                        }, client);

                        _Tracer.TraceVerbose("Sent SMS message {0}", message?.Sid);
                    }
                }
                catch (ArgumentNullException)
                {
                    ;
                }
            }

            return Guid.Empty;

        }


    }
}
