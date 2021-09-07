﻿/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Http;
using SanteDB.Core.Services;
using SanteDB.Persistence.Diagnostics.Jira.Model;
using System;
using System.Collections.Generic;

namespace SanteDB.Persistence.Diagnostics.Jira.Configuration
{

    /// <summary>
    /// Represents a JIRA session credential
    /// </summary>
    internal class JiraCredentials : Credentials
    {

        // JIRA service configuration
        private JiraServiceConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<JiraServiceConfigurationSection>();

        // Authentication
        private JiraAuthenticationResponse m_authentication;

        /// <summary>
        /// Create JIRA credentials
        /// </summary>
        public JiraCredentials(JiraAuthenticationResponse response) : base(null)
        {
            this.m_authentication = response;
        }

        /// <summary>
        /// Get the HTTP headers
        /// </summary>
        public override Dictionary<string, string> GetHttpHeaders()
        {
            return new Dictionary<string, string>()
            {
                { "Cookie", String.Format("{0}={1}", this.m_authentication.Session.Name, this.m_authentication.Session.Value) }
            };
        }
    }
}