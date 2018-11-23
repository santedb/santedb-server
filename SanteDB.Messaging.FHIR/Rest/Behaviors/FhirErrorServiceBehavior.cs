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
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using MARC.HI.EHRS.SVC.Core.Services;
using RestSrvr;
using RestSrvr.Exceptions;
using RestSrvr.Message;
using SanteDB.Core.Configuration;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.IO;
using System.Linq;
using System.Security;

namespace SanteDB.Messaging.FHIR.Rest.Behavior
{
    /// <summary>
    /// Service behavior
    /// </summary>
    public class FhirErrorEndpointBehavior : IServiceBehavior, IServiceErrorHandler
    {

        private TraceSource m_tracer = new TraceSource("SanteDB.Messaging.FHIR");
        private SanteDBConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("santedb.core") as SanteDBConfiguration;

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
            this.m_tracer.TraceEvent(TraceEventType.Error, error.HResult, "Error on WCF FHIR Pipeline: {0}", error);
            // Formulate appropriate response
            if (error is DomainStateException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.ServiceUnavailable;
            else if (error is PolicyViolationException || error is SecurityException)
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
            else if (error is SecurityTokenException)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                RestOperationContext.Current.OutgoingResponse.AddHeader("WWW-Authenticate", $"Bearer realm=\"{this.m_configuration.Security.ClaimsAuth.Realm}\"");
            }
            else if (error is UnauthorizedRequestException)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
                RestOperationContext.Current.OutgoingResponse.AddHeader("WWW-Authenticate", (error as UnauthorizedRequestException).AuthenticateChallenge);
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
            else
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;

            // Construct an error result
            var errorResult = new OperationOutcome()
            {
                Issue = new List<Issue>()
            {
                new Issue() { Diagnostics  = error.Message, Severity = IssueSeverity.Error }
            }
            };

            // Cascade inner exceptions
            var ie = error.InnerException;
            while (ie != null)
            {
                errorResult.Issue.Add(new Issue() { Diagnostics = String.Format("Caused by {0}", error.Message), Severity = IssueSeverity.Error });
                ie = ie.InnerException;
            }

            // Return error in XML only at this point
            new FhirMessageDispatchFormatter().SerializeResponse(response, null, errorResult);
            return true;
        }
    }
}
