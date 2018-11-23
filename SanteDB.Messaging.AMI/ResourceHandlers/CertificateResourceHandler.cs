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
 * Date: 2018-11-23
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Messaging.AMI.Wcf;
using SanteDB.Core.Model.AMI.Security;
using MARC.Util.CertificateTools;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Messaging.AMI.Configuration;
using System.Security.Cryptography.Pkcs;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.AMI;
using RestSrvr;

namespace SanteDB.Messaging.AMI.ResourceHandlers
{
    /// <summary>
    /// Represents a certificate resource handler for AMI
    /// </summary>
    public class CertificateResourceHandler : IResourceHandler
    {

        // Certificate tool 
        private CertTool m_certTool = null;
        // Configuration
        private readonly AmiConfiguration configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("santedb.messaging.ami") as AmiConfiguration;

        /// <summary>
        /// Creates a new certificate resource handler
        /// </summary>
        public CertificateResourceHandler()
        {
            this.m_certTool = new CertTool
            {
                CertificationAuthorityName = this.configuration?.CaConfiguration.Name,
                ServerName = this.configuration?.CaConfiguration.ServerName
            };
        }

        /// <summary>
        /// Get the capabilities for the handler
        /// </summary>
        public ResourceCapability Capabilities => ResourceCapability.Delete | ResourceCapability.Get | ResourceCapability.Search;

        /// <summary>
        /// Get the resource name
        /// </summary>
        public string ResourceName => "Certificate";

        /// <summary>
        /// Get the scope of this handler
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the type that this resource handler works with
        /// </summary>
        public Type Type => typeof(Object);

        /// <summary>
        /// Create a certificate is not supported
        /// </summary>
        /// <param name="data"></param>
        /// <param name="updateIfExists"></param>
        /// <returns></returns>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the specified certificate information
        /// </summary>
        /// <param name="rawId"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public object Get(object rawId, object versionId)
        {
            var id = int.Parse(rawId.ToString());

            RestOperationContext.Current.OutgoingResponse.ContentType = "application/x-pkcs12";
            RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition", $"attachment; filename=\"crt-{id}.p12\"");

            var result = this.m_certTool.GetRequestStatus(id);

            return Encoding.UTF8.GetBytes(result.AuthorityResponse);
        }

        /// <summary>
        /// Delete the certificate request
        /// </summary>
        /// <param name="key">The key of the certificate to revoke</param>
        /// <returns>The revoked certificate</returns>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public object Obsolete(object key)
        {
            // Revoke reason
            var strReason = RestOperationContext.Current.IncomingRequest.Headers["X-SanteDB-RevokeReason"] ??
                   RestOperationContext.Current.IncomingRequest.QueryString["reason"];
            var reason = (SanteDB.Core.Model.AMI.Security.RevokeReason)Enum.Parse(typeof(SanteDB.Core.Model.AMI.Security.RevokeReason), strReason);
            int id = Int32.Parse(key.ToString());
            var result = this.m_certTool.GetRequestStatus(id);

            if (String.IsNullOrEmpty(result.AuthorityResponse))
                throw new InvalidOperationException("Cannot revoke an un-issued certificate");
            // Now get the serial key
            SignedCms importer = new SignedCms();
            importer.Decode(Convert.FromBase64String(result.AuthorityResponse));

            foreach (var cert in importer.Certificates)
                if (cert.Subject != cert.Issuer)
                    this.m_certTool.RevokeCertificate(cert.SerialNumber, (MARC.Util.CertificateTools.RevokeReason)reason);

            result.Outcome = SubmitOutcome.Revoked;
            result.AuthorityResponse = null;
            return new SubmissionResult(result.Message, result.RequestId, (SubmissionStatus)result.Outcome, result.AuthorityResponse);
        }

        /// <summary>
        /// Query for all certificates
        /// </summary>
        /// <param name="queryParameters"></param>
        /// <returns></returns>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Query for certificates with limits
        /// </summary>
        /// <param name="queryParameters">The filter parameters</param>
        /// <param name="offset">The offset</param>
        /// <param name="count">The number to return</param>
        /// <param name="totalCount">The total results</param>
        /// <returns>The filtered result set</returns>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            totalCount = 0;
            return this.m_certTool.GetCertificates()
                .Select(ci => new X509Certificate2Info(ci.Attribute))
                .Where(ci => this.m_certTool.GetRequestStatus(ci.Id).Outcome == SubmitOutcome.Issued)
                .Skip(offset)
                .Take(count)
                .OfType<Object>();
        }

        /// <summary>
        /// Update a certificate
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
