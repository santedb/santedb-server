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

namespace SanteDB.Security.Tfa.Twilio
{
    public class TfaSmsMechanism : ITfaMechanism
    {
        private static readonly Guid s_TfaMechanismId = Guid.Parse("08124835-6C24-43C9-8650-9D605F6B5BD6");
        private static readonly string s_TemplateName = "org.santedb.notifications.mfa.sms";

        readonly INotificationService _NotificationService;
        readonly ITfaCodeProvider _TfaCodeProvider;
        readonly ITfaSecretManager _TfaSecretManager;

        public TfaSmsMechanism(INotificationService notificationService, ITfaCodeProvider tfaCodeProvider, ITfaSecretManager secretManager)
        {
            _NotificationService = notificationService;
            _TfaCodeProvider = tfaCodeProvider;
            _TfaSecretManager = secretManager;
        }

        public Guid Id => s_TfaMechanismId;

        public string Name => "org.santedb.tfa.sms";

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
                    throw new InvalidOperationException("SMS TFA requires telephone registered");
                }
                else if (!tonumber.StartsWith("sms:", StringComparison.OrdinalIgnoreCase))
                {
                    tonumber = "sms:" + tonumber;
                }


                string secret = null; 
                try
                {
                    secret = _TfaCodeProvider.GenerateTfaCode(ci);
                }
                catch (ArgumentException)
                {
                    secret = _TfaSecretManager.StartTfaRegistration(ci, 6, AuthenticationContext.SystemPrincipal);
                    _TfaSecretManager.FinishTfaRegistration(ci, secret, AuthenticationContext.SystemPrincipal);
                    secret = _TfaCodeProvider.GenerateTfaCode(ci);
                }

                var templatemodel = new Dictionary<string, object>
                {
                    {"user", user },
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
