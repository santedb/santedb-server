﻿using SanteDB.Core.Applets.ViewModel;
using SanteDB.Core.Applets.ViewModel.Json;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Export;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Tools.DataSandbox.Wcf
{
    /// <summary>
    /// Query tool behavior
    /// </summary>
    [ServiceBehavior(ConfigurationName = "DataSandboxTool")]
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

            WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
            WebOperationContext.Current.OutgoingResponse.Headers["Content-Disposition"] = "attachment; filename=codesystem.dataset";

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
                    WebOperationContext.Current.OutgoingResponse.ContentType = this.GetContentType(cpath);
                    return File.OpenRead(cpath);
                }
                else
                {
                    // Get the query tool stream
#if DEBUG
                    var contentPath = Path.Combine(Path.GetDirectoryName(typeof(DataSandboxTool).Assembly.Location), "SandboxTool", filename);

                    if (!File.Exists(contentPath))
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
                        return null;
                    }
                    else
                    {

                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                        WebOperationContext.Current.OutgoingResponse.ContentLength = new FileInfo(contentPath).Length;
                        WebOperationContext.Current.OutgoingResponse.ContentType = this.GetContentType(contentPath);

                        return File.OpenRead(contentPath);
                    }
#else
                    var contentPath = $"SanteDB.Tools.DataSandbox.Resources.{filename.Replace("/", ".")}";

                    if (!typeof(DataSandboxTool).Assembly.GetManifestResourceNames().Contains(contentPath))
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
                        return null;
                    }
                    else
                    {

                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                        //WebOperationContext.Current.OutgoingResponse.ContentLength = new FileInfo(contentPath).Length;
                        WebOperationContext.Current.OutgoingResponse.ContentType = this.GetContentType(contentPath);

                        return typeof(DataSandboxTool).Assembly.GetManifestResourceStream(contentPath);
                    }
#endif
                }
            }
            catch(Exception e)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;

                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                return null;
            }

        }

        /// <summary>
        /// Get the content type of the file
        /// </summary>
        private string GetContentType(string filename)
        {


            string extension = Path.GetExtension(filename);
            switch (extension.Substring(1).ToLower())
            {
                case "htm":
                case "html":
                    return "text/html";
                case "js":
                    return "application/javascript";
                case "css":
                    return "text/css";
                case "svg":
                    return "image/svg+xml";
                case "ttf":
                    return "application/x-font-ttf";
                case "eot":
                    return "application/vnd.ms-fontobject";
                case "woff":
                    return "application/font-woff";
                case "woff2":
                    return "application/font-woff2";
                case "gif":
                    return "image/gif";
                case "ico":
                    return "image/x-icon";
                case "png":
                    return "image/png";
                default:
                    return "application/x-octet-stream";
            }

        }
    }
}
