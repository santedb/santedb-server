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

namespace SanteDB.Security.Tfa.Twilio
{
    public class TfaTelMechanism : ITfaMechanism
    {
        private static readonly Guid s_TfaMechanismId = Guid.Parse("B94607B4-97A1-48A5-83B0-2F5C348299DC");
        private static readonly string s_TemplateName = "org.santedb.notifications.mfa.tel";

        readonly INotificationService _NotificationService;
        readonly ITfaCodeProvider _TfaCodeProvider;

        public TfaTelMechanism(INotificationService notificationService, ITfaCodeProvider tfaCodeProvider)
        {
            _NotificationService = notificationService;
            _TfaCodeProvider = tfaCodeProvider;
        }

        public Guid Id => s_TfaMechanismId;

        public string Name => "org.santedb.tfa.tel";

        public string Send(IIdentity user)
        {
            if (null == user)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user is IClaimsIdentity ci)
            {
                var tonumber = ci.GetFirstClaimValue(SanteDBClaimTypes.Telephone);

                if (string.IsNullOrWhiteSpace(tonumber))
                {
                    throw new InvalidOperationException("Tel TFA requires telephone registered");
                }
                else if (!tonumber.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
                {
                    tonumber = "tel:" + tonumber;
                }

                var secret = _TfaCodeProvider.GenerateTfaCode(ci);

                var templatemodel = new
                {
                    user,
                    tfa = secret,
                    secret,
                    code = secret,
                    principal = AuthenticationContext.Current.Principal
                };

                try
                {
                    _NotificationService.SendTemplatedNotification(new[] { tonumber }, s_TemplateName, CultureInfo.CurrentCulture.TwoLetterISOLanguageName, templatemodel, null, false);

                    return $"Code sent to ******{tonumber.Substring(tonumber.Length - 4, 4)}";
                }
                catch (Exception ex) when (!(ex is StackOverflowException || ex is OutOfMemoryException))
                {
                    throw new Exception("Error sending notification for tfa tel.", ex);
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot send notification to non-claims identity.");
            }
        }

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
    }
}
