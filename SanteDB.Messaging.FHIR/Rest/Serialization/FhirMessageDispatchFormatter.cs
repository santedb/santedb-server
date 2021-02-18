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
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Message;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Rest.Serialization
{
    /// <summary>
    /// Represents a dispatch message formatter which uses the JSON.NET serialization
    /// </summary>
    /// <remarks>This serialization is used because the SanteDB FHIR resources have extra features not contained in the pure HL7 API provided by HL7 International (such as operators to/from primitiives, generation of text, etc.). This 
    /// dispatch formatter is responsible for the serialization and de-serialization of FHIR objects to/from JSON and XML using the SanteDB classes for FHIR resources.</remarks>
    public class FhirMessageDispatchFormatter : IDispatchMessageFormatter
    {

        // Trace source
        private Tracer m_traceSource = new Tracer(FhirConstants.TraceSourceName);

        // Default settings
        private ParserSettings m_settings = new ParserSettings()
        {
            AcceptUnknownMembers = false,
            AllowUnrecognizedEnums = true,
            DisallowXsiAttributesOnRoot = false,
            PermissiveParsing = true
        };

        /// <summary>
        /// Creates a new instance of the FHIR message dispatch formatter
        /// </summary>
        public FhirMessageDispatchFormatter()
        {

        }

        /// <summary>
        /// Deserialize the request
        /// </summary>
        public void DeserializeRequest(EndpointOperation operation, RestRequestMessage request, object[] parameters)
        {

            try
            {
                var httpRequest = RestOperationContext.Current.IncomingRequest;
                string contentType = httpRequest.Headers["Content-Type"];

                for (int pNumber = 0; pNumber < parameters.Length; pNumber++)
                {
                    var parm = operation.Description.InvokeMethod.GetParameters()[pNumber];

                    // Simple parameter
                    if (parameters[pNumber] != null) continue;

                    // Use XML Serializer
                    if (contentType?.StartsWith("application/fhir+xml") == true)
                    {
                        var parser = new FhirXmlParser(this.m_settings);
                        using (var xr = XmlReader.Create(request.Body))
                            parameters[pNumber] = parser.Parse(xr);
                    }
                    // Use JSON Serializer
                    else if (contentType?.StartsWith("application/fhir+json") == true)
                    {
                        var parser = new FhirJsonParser(this.m_settings);
                        using (var sr = new StreamReader(request.Body))
                        using (var jr = new JsonTextReader(sr))
                            parameters[pNumber] = parser.Parse(jr);
                    }
                    else if (contentType != null)// TODO: Binaries
                        throw new InvalidOperationException("Invalid request format");
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                throw;
            }

        }

        /// <summary>
        /// Serialize the reply
        /// </summary>
        public void SerializeResponse(RestResponseMessage responseMessage, object[] parameters, object result)
        {
            try
            {
                // Outbound control
                var httpRequest = RestOperationContext.Current.IncomingRequest;
                string accepts = httpRequest.Headers["Accept"],
                    contentType = httpRequest.Headers["Content-Type"],
                    formatParm = httpRequest.QueryString["_format"];

                var summaryType = SummaryType.False;
                if (!RestOperationContext.Current.Data.TryGetValue("summaryType", out object sumaryTypeData))
                    summaryType = (SummaryType)sumaryTypeData;

                if (result is Base baseObject)
                {
                    // The request was in JSON or the accept is JSON
                    switch (accepts ?? contentType ?? formatParm)
                    {
                        case "application/fhir+xml":
                            using (var xw = XmlWriter.Create(responseMessage.Body))
                                new FhirXmlSerializer().Serialize(baseObject, xw, summaryType);
                            break;
                        case "application/fhir+json":
                            using (var sw = new StreamWriter(responseMessage.Body))
                            using (var jw = new JsonTextWriter(sw))
                                new FhirJsonSerializer().Serialize(baseObject, jw);
                            break;
                    }
                }
                else if (result == null)
                    responseMessage.StatusCode = 204; // no content
                else
                    throw new InvalidOperationException("FHIR return values must inherit from Base");

                RestOperationContext.Current.OutgoingResponse.ContentType = contentType;
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-PoweredBy", String.Format("{0} v{1} ({2})", Assembly.GetEntryAssembly().GetName().Name, Assembly.GetEntryAssembly().GetName().Version, Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion));
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-GeneratedOn", DateTime.Now.ToString("o"));
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                throw;
            }
        }
    }
}
