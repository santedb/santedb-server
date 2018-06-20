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
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.Services;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Selectors;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Wcf.Security
{
    /// <summary>
    /// Basic authorization credential validator for SanteDB basic auth
    /// </summary>
    public class BasicAuthorizationCredentialValidator : UserNamePasswordValidator
    {

        // Security trace source
        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.SecurityTraceSourceName);
        
        // Configuration from main SanteDB
        private SanteDBConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(SanteDBConstants.SanteDBConfigurationName) as SanteDBConfiguration;

        /// <summary>
        /// Validate the username and password
        /// </summary>
        public override void Validate(string userName, string password)
        {
            try
            {
                // Validation
                var authService = ApplicationContext.Current.GetService<IIdentityProviderService>();
                var principal = authService.Authenticate(userName, password);
                if (principal == null)
                    throw new UnauthorizedRequestException("Invalid username/password", "Basic", this.m_configuration.Security.BasicAuth.Realm, null);
            }
            catch(Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
            }
        }
    }
}
