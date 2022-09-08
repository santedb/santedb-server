/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using RestSrvr;
using RestSrvr.Attributes;
using SanteDB.Core.Applets.ViewModel.Json;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Export;
using SanteDB.Core.Model.Serialization;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace SanteDB.Tools.Debug.Wcf
{
    /// <summary>
    /// Query tool behavior
    /// </summary>
    [ExcludeFromCodeCoverage]
    [ServiceBehavior(Name = "HDSI_Sandbox")]
    public class DataSandboxTool : IDataSandboxTool
    {
        private readonly Tracer m_traceSource = new Tracer("SanteDB.Tools.Debug");

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
            XmlModelSerializerFactory.Current.CreateSerializer(typeof(Dataset)).Serialize(ms, output);
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
                    return typeof(DataSandboxTool).Assembly.GetManifestResourceStream($"{typeof(DataSandboxService).Namespace}.sandbox.config.json");
                }
                else
                {
                    // Get the query tool stream

                    var contentPath = $"{typeof(DataSandboxService).Namespace}.Resources.{filename.Replace("/", ".")}";

                    if (!typeof(DataSandboxTool).Assembly.GetManifestResourceNames().Contains(contentPath))
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 404;
                        return null;
                    }
                    else
                    {
                        RestOperationContext.Current.OutgoingResponse.StatusCode = 200; /// HttpStatusCode.OK;
                        //RestOperationContext.Current.OutgoingResponse.ContentLength = new FileInfo(contentPath).Length;
                        RestOperationContext.Current.OutgoingResponse.ContentType = DefaultContentTypeMapper.GetContentType(contentPath);

                        return typeof(DataSandboxTool).Assembly.GetManifestResourceStream(contentPath);
                    }
                }
            }
            catch (Exception e)
            {
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.InternalServerError;

                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                return null;
            }
        }
    }
}