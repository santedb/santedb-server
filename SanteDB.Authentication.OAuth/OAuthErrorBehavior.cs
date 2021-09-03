/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using Newtonsoft.Json;
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Authentication.OAuth2.Model;
using SanteDB.Core.Diagnostics;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text;

namespace SanteDB.Authentication.OAuth2
{
    /// <summary>
    /// Generate OAuth error behavior
    /// </summary>
    internal class OAuthErrorBehavior : IServiceBehavior, IServiceErrorHandler
    {

        private Tracer m_tracer = new Tracer(OAuthConstants.TraceSourceName);

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

            this.m_tracer.TraceEvent(EventLevel.Error, "Error on OAUTH Pipeline: {0}", error);

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