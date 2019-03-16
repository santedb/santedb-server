/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using MARC.Util.CertificateTools;
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Services;
using SanteDB.Messaging.AMI.Configuration;
using SanteDB.Rest.AMI;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Messaging.AMI.ResourceHandlers
{
    /// <summary>
    /// Represents a handler which is capable of storing and interacting with the windows CSR store
    /// </summary>
    public class CertificateSigningRequestHandler : IApiResourceHandler
    {

        // Certificate tool
        private readonly CertTool m_certTool;

        // Configuration
        private readonly AmiConfigurationSection configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AmiConfigurationSection>();

        /// <summary>
        /// Creates a new CSR request handler
        /// </summary>
        public CertificateSigningRequestHandler()
        {
            this.m_certTool = new CertTool
            {
                CertificationAuthorityName = this.configuration?.CaConfiguration.Name,
                ServerName = this.configuration?.CaConfiguration.ServerName
            };
        }

        /// <summary>
        /// Gets the capabilities of the current handler
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Delete;

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName => "Csr";

        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the logical type that this object handles
        /// </summary>
        public Type Type => typeof(SubmissionResult);

        /// <summary>
        /// Create a certificate signing request
        /// </summary>
        /// <param name="data">The certificate signing request request</param>
        /// <param name="updateIfExists">Not supported on this interface</param>
        /// <returns>The created CSR</returns>
        public object Create(object data, bool updateIfExists)
        {
            var s = data as SubmissionRequest;
            var submission = this.m_certTool.SubmitRequest(s.CmcRequest, s.AdminContactName, s.AdminAddress);

            var result = new SubmissionResult(submission.Message, submission.RequestId, (SubmissionStatus)submission.Outcome, submission.AuthorityResponse);
            if (this.configuration.CaConfiguration.AutoApprove)
                return this.Update(result);
            else
                return result;
        }

        /// <summary>
        /// Gets the specified CSR object
        /// </summary>
        public object Get(object id, object versionId)
        {
            int cid = Int32.Parse((string)id);
            var submission = this.m_certTool.GetRequestStatus(cid);

            var result = new SubmissionResult(submission.Message, submission.RequestId, (SubmissionStatus)submission.Outcome, submission.AuthorityResponse);
            return result;
        }

        /// <summary>
        /// Delete a CSR
        /// </summary>
        /// <param name="key">Deletes a CSR</param>
        /// <returns></returns>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Obsolete(object key)
        {
            int id = Int32.Parse(key.ToString());
            this.m_certTool.DenyRequest(id);
            var status = this.m_certTool.GetRequestStatus(id);

            var result = new SubmissionResult(status.Message, status.RequestId, (SubmissionStatus)status.Outcome, status.AuthorityResponse);
            result.Certificate = null;
            return result;
        }

        /// <summary>
        /// Query for CSRs
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Query for specific CSRs
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            List<SubmissionInfo> collection = new List<SubmissionInfo>();
            var certs = this.m_certTool.GetCertificates();
            foreach (var cert in certs)
            {
                SubmissionInfo info = SubmissionInfo.FromAttributes(cert.Attribute);
                info.XmlStatusCode = (SubmissionStatus)this.m_certTool.GetRequestStatus(Int32.Parse(info.RequestID)).Outcome;
                if (info.XmlStatusCode == SubmissionStatus.Submission)
                    collection.Add(info);
            }

            totalCount = collection.Count;
            return collection.OfType<Object>();
        }

        /// <summary>
        /// Updates the certificate signing request 
        /// </summary>
        /// <param name="data">The CSR to be update</param>
        /// <returns>The accepted certificate</returns>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Update(object data)
        {
            // Data
            var submissionRequest = data as SubmissionResult;
            this.m_certTool.Approve(submissionRequest.RequestId);
            var submission = this.m_certTool.GetRequestStatus(submissionRequest.RequestId);

            var result = new SubmissionResult(submission.Message, submission.RequestId, (SubmissionStatus)submission.Outcome, submission.AuthorityResponse);
            result.Certificate = null;
            return result;
        }
    }
}
