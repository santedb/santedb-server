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
 * Date: 2018-9-25
 */
using RestSrvr;
using RestSrvr.Attributes;
using SanteDB.Core.Applets.ViewModel.Json;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Export;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace SanteDB.Tools.DataSandbox.Wcf
{
    /// <summary>
    /// Query tool behavior
    /// </summary>
    [ServiceBehavior(Name = "DataSandboxTool")]
    public class DataSandboxTool : IDataSandboxTool
    {

        private TraceSource m_traceSource = new TraceSource("SanteDB.Tools.DataSandbox");

        /// <summary>
        /// Create dataset
        /// </summary>
        public Stream CreateDataset(Stream datasetSource)
        {
            Bundle input = new JsonViewModelSerializer().DeSerialize(datasetSource, typeof(Bundle)) as Bundle;
            Dataset output = new Dataset("Generated Dataset");
            output.Action = input.Item.Select(i => new DataUpdate()
            {
                InsertIfNotExists = true,
                Element = i
            }).OfType<DataInstallAction>().ToList();

            RestOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
            RestOperationContext.Current.OutgoingResponse.Headers["Content-Disposition"] = "attachment; filename=codesystem.dataset";

            MemoryStream ms = new MemoryStream();
            new XmlSerializer(typeof(Dataset)).Serialize(ms, output);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;

        }

        /// <summary>
        /// Get static content
        /// </summary>
        public Stream StaticContent(string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    content = "index.html";

                string filename = content.Contains("?")
                    ? content.Substring(0, content.IndexOf("?", StringComparison.Ordinal))
                    : content;

                if (filename == "config.json")
                {
                    var cpath = Path.Combine(Path.GetDirectoryName(typeof(DataSandboxTool).Assembly.Location), "sandbox.config.json");
                    RestOperationContext.Current.OutgoingResponse.ContentType = DefaultContentTypeMapper.GetContentType(cpath);
                    return File.OpenRead(cpath);
                }
                else
                {
                    // Get the query tool stream
#if DEBUG
                    var contentPath = Path.Combine(Path.GetDirectoryName(typeof(DataSandboxTool).Assembly.Location), "SandboxTool", filename);

                    if (!File.Exists(contentPath))
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NotFound;
                        return null;
                    }
                    else
                    {

                        RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.OK;
                        RestOperationContext.Current.OutgoingResponse.ContentLength64 = new FileInfo(contentPath).Length;
                        RestOperationContext.Current.OutgoingResponse.ContentType = DefaultContentTypeMapper.GetContentType(contentPath);

                        return File.OpenRead(contentPath);
                    }
#else
                    var contentPath = $"SanteDB.Tools.DataSandbox.Resources.{filename.Replace("/", ".")}";

                    if (!typeof(DataSandboxTool).Assembly.GetManifestResourceNames().Contains(contentPath))
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                        return null;
                    }
                    else
                    {

                        RestOperationContext.Current.OutgoingResponse.StatusCode = 200; /// HttpStatusCode.OK;
                        //RestOperationContext.Current.OutgoingResponse.ContentLength = new FileInfo(contentPath).Length;
                        RestOperationContext.Current.OutgoingResponse.ContentType = this.GetContentType(contentPath);

                        return typeof(DataSandboxTool).Assembly.GetManifestResourceStream(contentPath);
                    }
#endif
                }
            }
            catch(Exception e)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.InternalServerError;

                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                return null;
            }

        }

        
    }
}
