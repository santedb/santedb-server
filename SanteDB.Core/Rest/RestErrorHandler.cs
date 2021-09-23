/*
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

using RestSrvr;
using RestSrvr.Exceptions;
using RestSrvr.Message;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Security;
using SanteDB.Rest.Common.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Fault;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.Authentication;
using SanteDB.Core;
using System.IdentityModel.Tokens;
using System.Data.Linq;
using SanteDB.Server.Core.Configuration;
using SanteDB.Core.Security;

namespace SanteDB.Rest.Common.Serialization
{
    /// <summary>
    /// Error handler
    /// </summary>
    public class RestErrorHandler : IServiceErrorHandler
    {
        // Error tracer
        private Tracer m_traceSource = Tracer.GetTracer(typeof(RestErrorHandler));

        /// <summary>
        /// Handle error
        /// </summary>
        public bool HandleError(Exception error)
        {
            return true;
        }

        /// <summary>
        /// Provide fault
        /// </summary>
        public bool ProvideFault(Exception error, RestResponseMessage faultMessage)
        {
            var uriMatched = RestOperationContext.Current.IncomingRequest.Url;

            while (error.InnerException != null)
                error = error.InnerException;

            var fault = new RestServiceFault(error);

            // Formulate appropriate response
            if (error is DomainStateException)
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.ServiceUnavailable;
            else if (error is ObjectLockedException lockException)
            {
                faultMessage.StatusCode = 423;
                fault.Data.Add(lockException.LockedUser);
            }
            else if (error is PolicyViolationException)
            {
                var pve = error as PolicyViolationException;
                if (pve.PolicyDecision == PolicyGrantType.Elevate)
                {
                    // Ask the user to elevate themselves
                    faultMessage.StatusCode = 401;
                    var authHeader = $"{(RestOperationContext.Current.AppliedPolicies.OfType<BasicAuthorizationAccessBehavior>().Any() ? "Basic" : "Bearer")} realm=\"{RestOperationContext.Current.IncomingRequest.Url.Host}\" error=\"insufficient_scope\" scope=\"{pve.PolicyId}\"  error_description=\"{error.Message}\"";
                    RestOperationContext.Current.OutgoingResponse.AddHeader("WWW-Authenticate", authHeader);
                }
                else
                {
                    faultMessage.StatusCode = 403;
                }
            }
            else if (error is SecurityException)
            {
                faultMessage.StatusCode = (int)HttpStatusCode.Forbidden;
            }
            else if (error is SecurityTokenException)
            {
                // TODO: Audit this
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                var authHeader = $"Bearer realm=\"{RestOperationContext.Current.IncomingRequest.Url.Host}\" error=\"invalid_token\" error_description=\"{error.Message}\"";
                RestOperationContext.Current.OutgoingResponse.AddHeader("WWW-Authenticate", authHeader);
            }
            else if (error is LimitExceededException)
            {
                faultMessage.StatusCode = (int)(HttpStatusCode)429;
                faultMessage.StatusDescription = "Too Many Requests";
                faultMessage.Headers.Add("Retry-After", "1200");
            }
            else if (error is AuthenticationException)
            {
                var authHeader = $"{(RestOperationContext.Current.AppliedPolicies.OfType<BasicAuthorizationAccessBehavior>().Any() ? "Basic" : "Bearer")} realm=\"{RestOperationContext.Current.IncomingRequest.Url.Host}\" error=\"invalid_token\" error_description=\"{error.Message}\"";
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                RestOperationContext.Current.OutgoingResponse.AddHeader("WWW-Authenticate", authHeader);
            }
            else if (error is UnauthorizedAccessException)
            {
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            }
            else if (error is SecuritySessionException ses)
            {
                switch (ses.Type)
                {
                    case SessionExceptionType.Expired:
                    case SessionExceptionType.NotYetValid:
                    case SessionExceptionType.NotEstablished:
                        faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                        faultMessage.Headers.Add("WWW-Authenticate", $"Bearer");
                        break;

                    default:
                        faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                        break;
                }
            }
            else if (error is FaultException)
                faultMessage.StatusCode = (int)(error as FaultException).StatusCode;
            else if (error is Newtonsoft.Json.JsonException ||
                error is System.Xml.XmlException)
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            else if (error is DuplicateKeyException || error is DuplicateNameException)
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.Conflict;
            else if (error is FileNotFoundException || error is KeyNotFoundException)
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
            else if (error is DomainStateException)
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.ServiceUnavailable;
            else if (error is DetectedIssueException)
                faultMessage.StatusCode = (int)(System.Net.HttpStatusCode)422;
            else if (error is NotImplementedException)
                faultMessage.StatusCode = (int)HttpStatusCode.NotImplemented;
            else if (error is NotSupportedException)
                faultMessage.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            else if (error is PatchException)
                faultMessage.StatusCode = (int)HttpStatusCode.Conflict;
            else
                faultMessage.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;

            switch (faultMessage.StatusCode)
            {
                case 409:
                case 429:
                case 503:
                    this.m_traceSource.TraceInfo("Issue on REST pipeline: {0}", error);
                    break;

                case 401:
                case 403:
                case 501:
                case 405:
                    this.m_traceSource.TraceWarning("Warning on REST pipeline: {0}", error);
                    break;

                default:
                    this.m_traceSource.TraceError("Error on REST pipeline: {0}", error);
                    break;
            }

            RestMessageDispatchFormatter.CreateFormatter(RestOperationContext.Current.ServiceEndpoint.Description.Contract.Type).SerializeResponse(faultMessage, null, fault);
            AuditUtil.AuditNetworkRequestFailure(error, uriMatched, RestOperationContext.Current.IncomingRequest.Headers.AllKeys.ToDictionary(o => o, o => RestOperationContext.Current.IncomingRequest.Headers[o]), RestOperationContext.Current.OutgoingResponse.Headers.AllKeys.ToDictionary(o => o, o => RestOperationContext.Current.OutgoingResponse.Headers[o]));
            return true;
        }
    }
}