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
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Authentication.OAuth2.Model;

namespace SanteDB.Authentication.OAuth2
{
    /// <summary>
    /// Generate OAuth error behavior
    /// </summary>
    internal class OAuthErrorBehavior : IServiceBehavior, IServiceErrorHandler
    {

        private TraceSource m_tracer = new TraceSource(OAuthConstants.TraceSourceName);
        /// <summary>
        /// Apply the service behavior
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            dispatcher.ErrorHandlers.Clear();
            dispatcher.ErrorHandlers.Add(this);
        }

        /// <summary>
        /// Handle an error query
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

            this.m_tracer.TraceEvent(TraceEventType.Error, error.HResult ,"Error on OAUTH Pipeline: {0}", error);

            // Error
            OAuthError err = new OAuthError()
            {
                Error = OAuthErrorType.invalid_request,
                ErrorDescription = error.Message
            };

            JsonSerializer serializer = new JsonSerializer();

            response.ContentType = "application/json";
            using (var stw = new StringWriter())
            {
                serializer.Serialize(stw, err);
                response.Body = new MemoryStream(Encoding.UTF8.GetBytes(stw.ToString()));
            }

            return true;
        }
    }
}