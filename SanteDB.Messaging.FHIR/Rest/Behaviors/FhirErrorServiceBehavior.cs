/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using RestSrvr;
using RestSrvr.Exceptions;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Security;

namespace SanteDB.Messaging.FHIR.Rest.Behavior
{
    /// <summary>
    /// Service behavior
    /// </summary>
    public class FhirErrorEndpointBehavior :  IServiceBehavior, IServiceErrorHandler
    {

        private Tracer m_tracer = new Tracer(FhirConstants.TraceSourceName);

        /// <summary>
        /// Apply the service behavior
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.ErrorHandlers.Clear();
            dispatcher.ErrorHandlers.Add(this);
        }

        /// <summary>
        /// This error handle can handle all errors
        /// </summary>
        public bool HandleError(Exception error)
        {
            return true;
        }

        /// <summary>
        /// Provide a fault
        /// </summary>
        public bool ProvideFault(Exception error, RestResponseMessage response)
        {
            this.m_tracer.TraceEvent(EventLevel.Error, "Error on WCF FHIR Pipeline: {0}", error);

            // Get to the root of the error
            while (error.InnerException != null)
                error = error.InnerException;

            // Formulate appropriate response
            if (error is DomainStateException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.ServiceUnavailable;
            else if (error is PolicyViolationException)
            {
                var pve = error as PolicyViolationException;
                if (pve.PolicyDecision == PolicyGrantType.Elevate)
                {
                    // Ask the user to elevate themselves
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 401;
                    var authHeader = $"{(RestOperationContext.Current.AppliedPolicies.Any(o=>o.GetType().Name.Contains("Basic")) ? "Basic" : "Bearer")} realm=\"{RestOperationContext.Current.IncomingRequest.Url.Host}\" error=\"insufficient_scope\" scope=\"{pve.PolicyId}\"  error_description=\"{error.Message}\"";
                    RestOperationContext.Current.OutgoingResponse.AddHeader("WWW-Authenticate", authHeader);
                }
                else
                {
                    RestOperationContext.Current.OutgoingResponse.StatusCode = 403;
                }
            }
            else if (error is SecurityException || error is UnauthorizedAccessException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            else if (error is SecurityTokenException )
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                RestOperationContext.Current.OutgoingResponse.AddHeader("WWW-Authenticate", $"Bearer");
            }
            else if (error is FaultException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)(error as FaultException).StatusCode;
            else if (error is Newtonsoft.Json.JsonException ||
                error is System.Xml.XmlException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            else if (error is FileNotFoundException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.NotFound;
            else if (error is DbException || error is ConstraintException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)(System.Net.HttpStatusCode)422;
            else if (error is PatchException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.Conflict;

            else
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;

            // Construct an error result
            var errorResult = new OperationOutcome()
            {
                Issue = new List<Issue>()
            {
                new Issue() { Diagnostics  = error.Message, Severity = IssueSeverity.Error, Code = new FhirCoding(new Uri("http://hl7.org/fhir/issue-type"), "exception") }
            }
            };

            if (error is DetectedIssueException)
                foreach (var iss in (error as DetectedIssueException).Issues)
                    errorResult.Issue.Add(new Issue()
                    {
                        Diagnostics = iss.Text,
                        Severity = iss.Priority == DetectedIssuePriorityType.Error ? IssueSeverity.Error :
                        iss.Priority == DetectedIssuePriorityType.Warning ? IssueSeverity.Warning :
                        IssueSeverity.Information
                    });
            // Return error in XML only at this point
            new FhirMessageDispatchFormatter().SerializeResponse(response, null, errorResult);
            return true;
        }
    }
}
