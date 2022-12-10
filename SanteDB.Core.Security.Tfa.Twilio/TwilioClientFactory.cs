using SanteDB.Core.Security.Tfa.Twilio.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Twilio.Clients;

namespace SanteDB.Security.Tfa.Twilio
{
    public class TwilioClientFactory : IServiceImplementation
    {
        public string ServiceName => "Twilio Client Factory";

        readonly TwilioTfaMechanismConfigurationSection _Configuration;

        public TwilioClientFactory(IConfigurationManager configurationManager)
        {
            _Configuration = configurationManager.GetSection<TwilioTfaMechanismConfigurationSection>();
        }

        public ITwilioRestClient GetRestClient()
        {
            return new TwilioRestClient(_Configuration.Sid, _Configuration.Auth);
        }

        public string FromNumber => _Configuration.From;
    }
}
