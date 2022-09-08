/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core.Http;
using SanteDB.Core.Interop.Clients;
using SanteDB.Persistence.Diagnostics.Jira.Configuration;
using SanteDB.Persistence.Diagnostics.Jira.Model;
using System;
using System.Collections.Generic;

namespace SanteDB.Persistence.Diagnostics.Jira
{
    /// <summary>
    /// Represents a service client base
    /// </summary>
    public class JiraServiceClient : ServiceClientBase
    {
        /// <summary>
        /// Creates the specified service client
        /// </summary>
        public JiraServiceClient(IRestClient restClient) : base(restClient)
        {
        }

        /// <summary>
        /// Represents a JIRA authentication request
        /// </summary>
        public JiraAuthenticationResponse Authenticate(JiraAuthenticationRequest jiraAuthenticationRequest)
        {
            var result = this.Client.Post<JiraAuthenticationRequest, JiraAuthenticationResponse>("auth/1/session", "application/json", jiraAuthenticationRequest);
            this.Client.Credentials = new JiraCredentials(result);
            return result;
        }

        /// <summary>
        /// Create an issue
        /// </summary>
        public JiraIssueResponse CreateIssue(JiraIssueRequest issue)
        {
            return this.Client.Post<JiraIssueRequest, JiraIssueResponse>("api/2/issue", "application/json", issue);
        }

        /// <summary>
        /// Create an attachment
        /// </summary>
        public void CreateAttachment(JiraIssueResponse issue, MultipartAttachment attachment)
        {
            String boundary = String.Format("------{0:N}", Guid.NewGuid());
            this.Client.Post<MultipartAttachment, Object>(String.Format("api/2/issue/{0}/attachments", issue.Key), String.Format("multipart/form-data; boundary={0}", boundary), attachment);
        }

        /// <summary>
        /// Create an attachment
        /// </summary>
        public void CreateAttachment(JiraIssueResponse issue, List<MultipartAttachment> attachment)
        {
            String boundary = String.Format("------{0:N}", Guid.NewGuid());
            this.Client.Post<List<MultipartAttachment>, Object>(String.Format("api/2/issue/{0}/attachments", issue.Key), String.Format("multipart/form-data; boundary={0}", boundary), attachment);
        }
    }
}