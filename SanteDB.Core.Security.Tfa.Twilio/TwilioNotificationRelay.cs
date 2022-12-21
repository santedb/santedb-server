using SanteDB.Core.Diagnostics;
using SanteDB.Core.Notifications;
using System;
using System.Collections.Generic;
using System.Text;
using Twlo = global::Twilio;

namespace SanteDB.Security.Tfa.Twilio
{
    public class TwilioNotificationRelay : INotificationRelay
    {
        readonly TwilioClientFactory _ClientFactory;
        readonly Tracer _Tracer;

        public TwilioNotificationRelay(TwilioClientFactory factory)
        {
            _Tracer = new Tracer(nameof(TwilioNotificationRelay));
            _ClientFactory = factory;
        }

        public IEnumerable<string> SupportedSchemes => new[] { "sms", "tel" };

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
