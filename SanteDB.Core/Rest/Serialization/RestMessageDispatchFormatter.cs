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
 * User: fyfej
 * Date: 2017-9-1
 */
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using SanteDB.Core.Model;
using System.Xml.Schema;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Security;
using System.Diagnostics;
using SanteDB.Core.Model.Collection;
using Newtonsoft.Json.Converters;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Rest.Compression;
using SanteDB.Core.Model.Query;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Applets.ViewModel.Json;
using SanteDB.Core.Model.Json.Formatter;
using SanteDB.Core.Applets.ViewModel.Description;
using SanteDB.Core.Diagnostics;
using RestSrvr;
using SanteDB.Core.Rest.Serialization;
using RestSrvr.Message;

namespace SanteDB.Core.Rest.Serialization
{
    /// <summary>
    /// Represents a dispatch message formatter which uses the JSON.NET serialization
    /// </summary>
    public class RestMessageDispatchFormatter : IDispatchMessageFormatter
    {

        private String m_version = Assembly.GetEntryAssembly().GetName().Version.ToString();
        private String m_versionName = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unnamed";
        // Trace source
        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.WcfTraceSourceName);
        // Serializers
        private static Dictionary<Type, XmlSerializer> s_serializers = new Dictionary<Type, XmlSerializer>();
        // Default view model
        private static ViewModelDescription m_defaultViewModel = null;

        // Static ctor
        static RestMessageDispatchFormatter()
        {
            m_defaultViewModel = ViewModelDescription.Load(typeof(RawBodyWriter).Assembly.GetManifestResourceStream("SanteDB.Core.Resources.ViewModel.xml"));
            var tracer = new TraceSource(SanteDBConstants.WcfTraceSourceName);
        }

        /// <summary>
        /// Deserialize the request
        /// </summary>
        public void DeserializeRequest(EndpointOperation operation, RestRequestMessage request, object[] parameters)
        {

            try
            {
#if DEBUG
                this.m_traceSource.TraceEvent(TraceEventType.Information, 0, "Received request from: {0}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint);
#endif

                var httpRequest = RestOperationContext.Current.IncomingRequest;
                string contentType = httpRequest.Headers["Content-Type"];

                for (int pNumber = 0; pNumber < parameters.Length; pNumber++)
                {
                    var parm = operation.Description.InvokeMethod.GetParameters()[pNumber];

                    // Simple parameter
                    if (parameters[pNumber] != null)
                    {
                        continue; // dispatcher already populated
                    }
                    // Use XML Serializer
                    else if (contentType?.StartsWith("application/xml") == true)
                    {
                        XmlSerializer serializer = null;
                        if (!s_serializers.TryGetValue(parm.ParameterType, out serializer))
                        {
                            serializer = new XmlSerializer(parm.ParameterType);
                            lock (s_serializers)
                                if (!s_serializers.ContainsKey(parm.ParameterType))
                                    s_serializers.Add(parm.ParameterType, serializer);
                        }
                        var requestObject = serializer.Deserialize(request.Body);
                        parameters[pNumber] = requestObject;
                    }
                    else if (contentType?.StartsWith("application/json+sdb-viewmodel") == true && typeof(IdentifiedData).IsAssignableFrom(parm.ParameterType))
                    {
                        var viewModel = httpRequest.Headers["X-SanteDB-ViewModel"] ?? httpRequest.QueryString["_viewModel"];

                        // Create the view model serializer
                        var viewModelSerializer = new JsonViewModelSerializer();
                        viewModelSerializer.LoadSerializerAssembly(typeof(ActExtensionViewModelSerializer).Assembly);

                        if (!String.IsNullOrEmpty(viewModel))
                        {
                            var viewModelDescription = ApplicationContext.Current.GetService<IAppletManagerService>()?.Applets.GetViewModelDescription(viewModel);
                            viewModelSerializer.ViewModel = viewModelDescription;
                        }
                        else
                        {
                            viewModelSerializer.ViewModel = m_defaultViewModel;
                        }

                        using (var sr = new StreamReader(request.Body))
                            parameters[pNumber] = viewModelSerializer.DeSerialize(sr, parm.ParameterType);
                    }
                    else if (contentType?.StartsWith("application/json") == true)
                    {
                        using (var sr = new StreamReader(request.Body))
                        {
                            JsonSerializer jsz = new JsonSerializer()
                            {
                                SerializationBinder = new ModelSerializationBinder(),
                                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                                TypeNameHandling = TypeNameHandling.All
                            };
                            jsz.Converters.Add(new StringEnumConverter());
                            var dserType = parm.ParameterType;
                            parameters[pNumber] = jsz.Deserialize(sr, dserType);
                        }
                    }
                    else if (contentType == "application/octet-stream")
                    {
                        parameters[pNumber] = request.Body;
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
        public void SerializeResponse(RestResponseMessage response, object[] parameters, object result)
        {
            try
            {
                // Outbound control
                var httpRequest = RestOperationContext.Current.IncomingRequest;
                string accepts = httpRequest.Headers["Accept"],
                    contentType = httpRequest.Headers["Content-Type"];

                // Result is serializable
                if (result?.GetType().GetCustomAttribute<XmlTypeAttribute>() != null ||
                    result?.GetType().GetCustomAttribute<JsonObjectAttribute>() != null)
                {
                    // The request was in JSON or the accept is JSON
                    if (accepts?.StartsWith("application/json+sdb-viewmodel") == true &&
                        typeof(IdentifiedData).IsAssignableFrom(result?.GetType()))
                    {
                        var viewModel = httpRequest.Headers["X-SanteDB-ViewModel"] ?? httpRequest.QueryString["_viewModel"];

                        // Create the view model serializer
                        var viewModelSerializer = new JsonViewModelSerializer();
                        viewModelSerializer.LoadSerializerAssembly(typeof(ActExtensionViewModelSerializer).Assembly);

                        if (!String.IsNullOrEmpty(viewModel))
                        {
                            var viewModelDescription = ApplicationContext.Current.GetService<IAppletManagerService>()?.Applets.GetViewModelDescription(viewModel);
                            viewModelSerializer.ViewModel = viewModelDescription;
                        }
                        else
                        {
                            viewModelSerializer.ViewModel = m_defaultViewModel;
                        }

                        using (var tms = new MemoryStream())
                        using (StreamWriter sw = new StreamWriter(tms, Encoding.UTF8))
                        using (JsonWriter jsw = new JsonTextWriter(sw))
                        {
                            viewModelSerializer.Serialize(jsw, result as IdentifiedData);
                            jsw.Flush();
                            sw.Flush();
                            response.Body = new MemoryStream(tms.ToArray());
                        }

                        contentType = "application/json+oiz-viewmodel";
                    }
                    else if (accepts?.StartsWith("application/json") == true ||
                        contentType?.StartsWith("application/json") == true)
                    {
                        // Prepare the serializer
                        JsonSerializer jsz = new JsonSerializer();
                        jsz.Converters.Add(new StringEnumConverter());

                        // Write json data
                        using (MemoryStream ms = new MemoryStream())
                        using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                        using (JsonWriter jsw = new JsonTextWriter(sw))
                        {
                            jsz.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                            jsz.NullValueHandling = NullValueHandling.Ignore;
                            jsz.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                            jsz.TypeNameHandling = TypeNameHandling.Auto;
                            jsz.Converters.Add(new StringEnumConverter());
                            jsz.Serialize(jsw, result);
                            jsw.Flush();
                            sw.Flush();
                            response.Body = new MemoryStream(ms.ToArray());

                        }

                        // Prepare reply for the WCF pipeline
                        contentType = "application/json";
                    }
                    // The request was in XML and/or the accept is JSON
                    else
                    {
                        XmlSerializer xsz = null;
                        if (!s_serializers.TryGetValue(result.GetType(), out xsz))
                        {
                            // Build a serializer
                            this.m_traceSource.TraceWarning("Could not find pre-created serializer for {0}, will generate one...", result.GetType().FullName);
                            xsz = new XmlSerializer(result.GetType());
                            lock (s_serializers)
                            {
                                if (!s_serializers.ContainsKey(result.GetType()))
                                    s_serializers.Add(result.GetType(), xsz);
                            }
                        }

                        MemoryStream ms = new MemoryStream();
                        xsz.Serialize(ms, result);
                        contentType = "application/xml";
                        ms.Seek(0, SeekOrigin.Begin);
                        response.Body = ms;
                    }
                }
                else if (result is XmlSchema)
                {
                    MemoryStream ms = new MemoryStream();
                    (result as XmlSchema).Write(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    contentType = "text/xml";
                    response.Body = ms;
                }
                else if (result is Stream) // TODO: This is messy, clean it up
                {
                    contentType = "application/octet-stream";
                    response.Body = result as Stream;
                }
                else {
                    contentType = "text/plain";
                    response.Body = new MemoryStream(Encoding.UTF8.GetBytes(result.ToString()));
                }

                RestOperationContext.Current.OutgoingResponse.ContentType = RestOperationContext.Current.OutgoingResponse.ContentType ?? contentType;
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-PoweredBy", String.Format("SanteDB {0} ({1})", m_version, m_versionName));
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-GeneratedOn", DateTime.Now.ToString("o"));
                AuthenticationContext.Current = null;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                new RestErrorHandler().ProvideFault(e, response);
            }
        }
    }
}
