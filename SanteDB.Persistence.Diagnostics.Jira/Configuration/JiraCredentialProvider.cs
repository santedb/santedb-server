using SanteDB.Core.Http;
using SanteDB.Persistence.Diagnostics.Jira.Model;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Principal;

namespace SanteDB.Persistence.Diagnostics.Jira.Configuration
{
    /// <summary>
    /// Credential provider for JIRA
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class JiraCredentialProvider : ICredentialProvider
    {
        public const string UserNameConfigurationProperty = "username";
        public const string PasswordConfigurationProperty = "password";
        private JiraAuthenticationResponse m_authentication;

        /// <summary>
        /// Authenticate the JIRA client
        /// </summary>
        public RestRequestCredentials Authenticate(IRestClient context)
        {
            var credentialProperty = context.Description.Binding.Security.CredentialProviderParameters;
            if (!credentialProperty.TryGetValue(UserNameConfigurationProperty, out string userName) || !credentialProperty.TryGetValue(PasswordConfigurationProperty, out string password))
            {
                throw new InvalidOperationException("Endpoint configuraiton is missing username and password property");
            }

            var result = context.Post<JiraAuthenticationRequest, JiraAuthenticationResponse>("auth/1/session", "application/json", new JiraAuthenticationRequest(userName, password));
            this.m_authentication = result;
            return new JiraCredentials(this.m_authentication);
        }

        /// <summary>
        /// Get current credentials
        /// </summary>
        public RestRequestCredentials GetCredentials(IRestClient context) => this.Authenticate(context);

        /// <summary>
        /// Not supported to get credentials on this endpoint via principal
        /// </summary>
        public RestRequestCredentials GetCredentials(IPrincipal principal)
        {
            throw new NotImplementedException();
        }

    }
}
