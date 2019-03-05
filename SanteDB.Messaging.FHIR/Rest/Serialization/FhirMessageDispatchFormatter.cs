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
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Newtonsoft.Json;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Message;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class FhirMessageDispatchFormatter : IDispatchMessageFormatter
    {

        // Trace source
        private TraceSource m_traceSource = new TraceSource("SanteDB.Messaging.FHIR");
        // Known types
        private static Type[] s_knownTypes = typeof(IFhirServiceContract).GetCustomAttributes<ServiceKnownResourceAttribute>().Select(o=>o.Type).ToArray();

        // Serializers
        private static Dictionary<Type, XmlSerializer> s_serializers = new Dictionary<Type, XmlSerializer>();

        // Static ctor
        static FhirMessageDispatchFormatter()
        {
            foreach (var s in s_knownTypes)
                s_serializers.Add(s, new XmlSerializer(s,  s.GetCustomAttributes<XmlIncludeAttribute>().Select(o => o.Type).ToArray()));
        }

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
                        XmlSerializer serializer = null;
                        using (XmlReader bodyReader = XmlReader.Create(request.Body))
                        {
                            while (bodyReader.NodeType != XmlNodeType.Element)
                                bodyReader.Read();

                            Type eType = s_knownTypes.FirstOrDefault(o => o.GetCustomAttribute<XmlRootAttribute>()?.ElementName == bodyReader.LocalName &&
                                o.GetCustomAttribute<XmlRootAttribute>()?.Namespace == bodyReader.NamespaceURI);
                            if (!s_serializers.TryGetValue(eType, out serializer))
                            {
                                serializer = new XmlSerializer(eType);
                                lock (s_serializers)
                                    if (!s_serializers.ContainsKey(eType))
                                        s_serializers.Add(eType, serializer);
                            }
                            parameters[pNumber] = serializer.Deserialize(request.Body);
                        }

                    }
                    // Use JSON Serializer
                    else if (contentType?.StartsWith("application/fhir+json") == true)
                    {

                        // Now read the JSON data
                        Object fhirObject = null;
                        using (StreamReader sr = new StreamReader(request.Body))
                        {
                            string fhirContent = sr.ReadToEnd();
                            fhirObject = FhirParser.ParseFromJson(fhirContent);
                        }

                        // Now we want to serialize the FHIR MODEL object and re-parse as our own API bundle object
                        MemoryStream ms = new MemoryStream(FhirSerializer.SerializeResourceToXmlBytes(fhirObject as Hl7.Fhir.Model.Resource));
                        XmlSerializer xsz = s_serializers[fhirObject?.GetType()];
                        parameters[0] = xsz.Deserialize(ms);
                    }
                    else if (contentType != null)// TODO: Binaries
                        throw new InvalidOperationException("Invalid request format");
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
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

                // Result is serializable
                if (result?.GetType().GetCustomAttribute<XmlTypeAttribute>() != null ||
                    result?.GetType().GetCustomAttribute<JsonObjectAttribute>() != null)
                {
                    XmlSerializer xsz = s_serializers[result.GetType()];
                    MemoryStream ms = new MemoryStream();
                    xsz.Serialize(ms, result);
                    contentType = "application/fhir+xml";
                    ms.Seek(0, SeekOrigin.Begin);
                    // The request was in JSON or the accept is JSON
                    if (accepts?.StartsWith("application/fhir+json") == true ||
                        contentType?.StartsWith("application/fhir+json") == true ||
                        formatParm?.Contains("application/fhir+json") == true)
                    {

                        // Parse XML object
                        Object fhirObject = null;
                        using (StreamReader sr = new StreamReader(ms))
                        {
                            String fhirContent = sr.ReadToEnd();
                            var parser = new FhirXmlParser();
                            parser.Settings.AllowUnrecognizedEnums = true;
                            parser.Settings.DisallowXsiAttributesOnRoot = false;
                            fhirObject = parser.Parse<Resource>(fhirContent);
                        }

                        // Now we serialize to JSON
                        byte[] body = FhirSerializer.SerializeResourceToJsonBytes(fhirObject as Hl7.Fhir.Model.Resource);
                        ms.Dispose();
                        ms = new MemoryStream(body);
                        // Prepare reply for the WCF pipeline
                        contentType = "application/fhir+json";
                    }
                    responseMessage.Body = ms;
                }
                else if (result is XmlSchema)
                {
                    MemoryStream ms = new MemoryStream();
                    (result as XmlSchema).Write(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    contentType = "text/xml";
                    responseMessage.Body = ms;
                }
                else if (result is Stream) // TODO: This is messy, clean it up
                {
                    responseMessage.Body = result as Stream;
                }

                
                RestOperationContext.Current.OutgoingResponse.ContentType = contentType;
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-PoweredBy", String.Format("{0} v{1} ({2})", Assembly.GetEntryAssembly().GetName().Name, Assembly.GetEntryAssembly().GetName().Version, Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion));
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-GeneratedOn", DateTime.Now.ToString("o"));
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                throw;
            }
        }
    }
}
