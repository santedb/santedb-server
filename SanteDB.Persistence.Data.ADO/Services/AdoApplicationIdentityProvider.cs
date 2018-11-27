/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-6-22
 */
using SanteDB.Core;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Sql Application IdP
    /// </summary>
#pragma warning disable CS0067
    [ServiceProvider("ADO.NET Application Identity Provider")]
    public class AdoApplicationIdentityProvider : IApplicationIdentityProviderService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Application Identity Provider";

        // Trace source
        private TraceSource m_traceSource = new TraceSource(AdoDataConstants.IdentityTraceSourceName);

        // Configuration
        private AdoPersistenceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AdoPersistenceConfigurationSection>();

        /// <summary>
        /// Fired prior to an authentication request being made
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        /// <summary>
        /// Fired after an authentication request has been made
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// Authenticate the application identity to an application principal
        /// </summary>
        public IPrincipal Authenticate(string applicationId, string applicationSecret)
        {
            // Data context
            using (DataContext dataContext = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    dataContext.Open();
                    IPasswordHashingService hashService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

                    var client = dataContext.FirstOrDefault<DbSecurityApplication>("auth_app", applicationId, hashService.ComputeHash(applicationSecret), 5);
                    if (client == null)
                        throw new SecurityException("Invalid application credentials");
                    else if (client.Key == Guid.Empty)
                        throw new AuthenticationException(client.PublicId);

                    IPrincipal applicationPrincipal = new ApplicationPrincipal(new SanteDB.Core.Security.ApplicationIdentity(client.Key, client.PublicId, true));
                    new PolicyPermission(System.Security.Permissions.PermissionState.None, PermissionPolicyIdentifiers.LoginAsService, applicationPrincipal).Demand();
                    return applicationPrincipal;
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error authenticating {0} : {1}", applicationId, e);
                    throw new AuthenticationException("Error authenticating application", e);
                }
            }


        }

        /// <summary>
        /// Gets the specified identity
        /// </summary>
        public IIdentity GetIdentity(string name)
        {
            // Data context
            using (DataContext dataContext = this.m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    dataContext.Open();
                    var client = dataContext.FirstOrDefault<DbSecurityApplication>(o => o.PublicId == name);
                    return new SanteDB.Core.Security.ApplicationIdentity(client.Key, client.PublicId, false);

                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, "Error getting identity data for {0} : {1}", name, e);
                    throw;
                }

        }
    }
}
#pragma warning restore CS0067
