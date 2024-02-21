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
using SanteDB.Core.Notifications;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Principal;
using System.Text;
using Twilio.TwiML.Voice;
using System.Linq;
using SanteDB.Core;
using SanteDB.Core.Security.Tfa;

namespace SanteDB.Security.Tfa.Twilio
{
    /// <summary>
    /// Represents a <see cref="ITfaMechanism"/> which uses SMS text messages
    /// </summary>
    public class TfaSmsMechanism : ITfaMechanism
    {
        private static readonly Guid s_TfaMechanismId = Guid.Parse("08124835-6C24-43C9-8650-9D605F6B5BD6");
        private static readonly string s_TemplateName = "org.santedb.notifications.mfa.sms";

        readonly INotificationService _NotificationService;
        readonly ITfaCodeProvider _TfaCodeProvider;
        readonly ITfaSecretManager _TfaSecretManager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public TfaSmsMechanism(INotificationService notificationService, ITfaCodeProvider tfaCodeProvider, ITfaSecretManager secretManager)
        {
            _NotificationService = notificationService;
            _TfaCodeProvider = tfaCodeProvider;
            _TfaSecretManager = secretManager;
        }

        /// <inheritdoc/>
        public Guid Id => s_TfaMechanismId;

        /// <inheritdoc/>
        public string Name => "org.santedb.tfa.sms";

        /// <inheritdoc/>
        public SanteDBHostType[] HostTypes => new SanteDBHostType[] {
            SanteDBHostType.Server
        };

        /// <inheritdoc/>
        public TfaMechanismClassification Classification => TfaMechanismClassification.Message;

        /// <inheritdoc/>
        public string SetupHelpText => "org.santedb.tfa.sms.setup";

        /// <inheritdoc/>
        public string Send(IIdentity user)
        {
            if (null == user)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user is IClaimsIdentity ci)
            {
                var tonumber = this.GetTelephoneNumberOrThrow(ci);
                string secret =  _TfaCodeProvider.GenerateTfaCode(ci);
                return this.SendNotification(tonumber, secret, user.Name);
            }
            else
            {
                throw new InvalidOperationException("Cannot send notification to non-claims identity.");
            }
        }

        /// <summary>
        /// Get the telephone number from <paramref name="claimsIdentity"/> or throw <see cref="InvalidOperationException"/>
        /// </summary>
        private string GetTelephoneNumberOrThrow(IClaimsIdentity claimsIdentity)
        {
            var tonumber = claimsIdentity.GetFirstClaimValue(SanteDBClaimTypes.Telephone);

            if (string.IsNullOrWhiteSpace(tonumber))
            {
                throw new InvalidOperationException("SMS TFA requires telephone registered");
            }
            else if (!tonumber.StartsWith("sms:", StringComparison.OrdinalIgnoreCase))
            {
                return "sms:" + tonumber;
            }
            return tonumber;
        }

        /// <summary>
        /// Send the SMS message
        /// </summary>
        private string SendNotification(string tonumber, string secret, string userName)
        {

            var templatemodel = new Dictionary<string, object>
                {
                    {"user", userName },
                    { "tfa", secret },
                    {"secret", secret },
                    { "code", secret },
                    { "codeWithSpaces", string.Join(" ", secret.Select(s => s.ToString())) },
                    { "principal", AuthenticationContext.Current.Principal }
                };

            try
            {
                _NotificationService.SendTemplatedNotification(new[] { tonumber }, s_TemplateName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName, templatemodel, null, false);
                return $"Code sent to ******{tonumber.Substring(tonumber.Length - 4, 4)}";
            }
            catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
            {
                throw new Exception("Error sending notification for tfa sms.", ex);
            }
        }

        /// <inheritdoc/>
        public bool Validate(IIdentity user, string secret)
        {
            if (null == user)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ArgumentException("Invalid secret", nameof(secret));
            }

            return _TfaCodeProvider.VerifyTfaCode(user, secret, DateTimeOffset.UtcNow);
        }

        /// <inheritdoc/>
        public string BeginSetup(IIdentity user)
        {
            if (user is IClaimsIdentity ci)
            {
                this._TfaSecretManager.RemoveTfaRegistration(ci, AuthenticationContext.Current.Principal);
                var telephone = this.GetTelephoneNumberOrThrow(ci);
                var secret = _TfaSecretManager.StartTfaRegistration(ci, 6, Rfc4226Mode.TotpTenMinuteInterval, AuthenticationContext.SystemPrincipal);
                return this.SendNotification(telephone, secret, ci.Name);
            }
            else
            {
                throw new InvalidOperationException("Cannot send notification to non-claims identity.");
            }
        }

        /// <inheritdoc/>
        public bool EndSetup(IIdentity user, string verificationCode)
        {
            if (user is IClaimsIdentity ci)
            {
                var telephone = this.GetTelephoneNumberOrThrow(ci);
                return _TfaSecretManager.FinishTfaRegistration(ci, verificationCode, AuthenticationContext.SystemPrincipal);
            }
            else
            {
                throw new InvalidOperationException("Cannot send notification to non-claims identity.");
            }
        }
    }
}
