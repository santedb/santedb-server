﻿/*
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
using MARC.HI.EHRS.SVC.Core.Exceptions;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Wcf.Serialization;
using SanteDB.Messaging.HDSI.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Authentication;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.HDSI.Wcf.Serialization
{
    /// <summary>
    /// Error handler
    /// </summary>
    public class HdsiErrorHandler : IErrorHandler
    {
        // Trace source
        private TraceSource m_traceSource = new TraceSource("SanteDB.Messaging.HDSI.Wcf");

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
        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {

            this.m_traceSource.TraceEvent(TraceEventType.Error, error.HResult, "Error on HDSI WCF Pipeline: {0}", error);

            ErrorResult retVal = null;

            // Formulate appropriate response
            if (error is PolicyViolationException || error is SecurityException || (error as FaultException)?.Code.SubCode?.Name == "FailedAuthentication")
            {
                AuditUtil.AuditRestrictedFunction(error, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }
            else if (error is SecurityTokenException)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;
                WebOperationContext.Current.OutgoingResponse.Headers.Add("WWW-Authenticate", "Bearer");
            }
            else if (error is FileNotFoundException || error is KeyNotFoundException)
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotFound;
            else if (error is WebFaultException)
                WebOperationContext.Current.OutgoingResponse.StatusCode = (error as WebFaultException).StatusCode;
            else if (error is Newtonsoft.Json.JsonException ||
                error is System.Xml.XmlException)
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.BadRequest;
            else if (error is LimitExceededException)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = (HttpStatusCode)429;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = "Too Many Requests";
                WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.RetryAfter, "1200");

            }
            else if (error is UnauthorizedRequestException)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;
                WebOperationContext.Current.OutgoingResponse.Headers.Add("WWW-Authenticate", (error as UnauthorizedRequestException).AuthenticateChallenge);
            }
            else if (error is UnauthorizedAccessException)
            {
                AuditUtil.AuditRestrictedFunction(error as UnauthorizedAccessException, WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Forbidden;
            }
            else if (error is DomainStateException)
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
            else if (error is DetectedIssueException)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = (System.Net.HttpStatusCode)422;
                retVal = new ErrorResult()
                {
                    Key = Guid.NewGuid(),
                    Type = "BusinessRuleViolation",
                    Details = (error as DetectedIssueException).Issues.Select(o => new ResultDetail(o.Priority == Core.Services.DetectedIssuePriorityType.Error ? DetailType.Error : o.Priority == Core.Services.DetectedIssuePriorityType.Warning ? DetailType.Warning : DetailType.Information, o.Text)).ToList()
                };
            }
            else if (error is PatchAssertionException)
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Conflict;
            else if (error is PatchException)
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NotAcceptable;

            else
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;

            // Construct an error result
            if (retVal == null)
                retVal = new ErrorResult()
                {
                    Key = Guid.NewGuid(),
                    Type = error.GetType().Name,
                    Details = new List<ResultDetail>()
                    {
                        new ResultDetail(DetailType.Error, error.Message)
                    }
                };

            // Cascade inner exceptions
            var ie = error.InnerException;
            while (ie != null)
            {
                retVal.Details.Add(new ResultDetail(DetailType.Error, String.Format("Caused By: {0}", ie.Message)));
                ie = ie.InnerException;
            }
            // Return error in XML only at this point
            fault = new WcfMessageDispatchFormatter<IHdsiServiceContract>().SerializeReply(version, null, retVal);


        }
    }
}
