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
 * User: fyfej
 * Date: 2017-9-1
 */
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using SanteDB.Persistence.Data.ADO.Data.Model;
using System.Diagnostics;
using SanteDB.Persistence.Data.ADO.Configuration;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Event;
using System.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.OrmLite;
using System.Security.Authentication;
using SanteDB.Persistence.Data.ADO.Data;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Sql Application IdP
    /// </summary>
#pragma warning disable CS0067
    public class AdoApplicationIdentityProvider : IApplicationIdentityProviderService
    {
        // Trace source
        private TraceSource m_traceSource = new TraceSource(AdoDataConstants.IdentityTraceSourceName);

        // Configuration
        private AdoConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(AdoDataConstants.ConfigurationSectionName) as AdoConfiguration;

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
                    IPasswordHashingService hashService = ApplicationContext.Current.GetService<IPasswordHashingService>();

                    var client = dataContext.FirstOrDefault<DbSecurityApplication>("auth_app", applicationId, hashService.EncodePassword(applicationSecret));
                    if (client == null)
                        throw new SecurityException("Invalid application credentials");

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
