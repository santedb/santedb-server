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
using SanteDB.Core;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.AMI.Configuration;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Permissions;
using SanteDB.Server.Core.Services;
using SanteDB.Server.Core.Configuration;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Diagnostics.Tracing;

namespace SanteDB.Messaging.AMI.Wcf
{
    /// <summary>
    /// Administration Management Interface (AMI)
    /// </summary>
    /// <remarks>Represents the SanteDB Server implementation of the Administrative Management Interface (AMI) contract</remarks>
    public class AmiServiceBehavior : AmiServiceBehaviorBase
    {
        /// <summary>
        /// Get resource handler
        /// </summary>
        protected override ResourceHandlerTool GetResourceHandler() => AmiMessageHandler.ResourceHandler;

        /// <summary>
        /// Get a list of TFA mechanisms
        /// </summary>
        /// <returns>Returns a list of TFA mechanisms.</returns>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public override AmiCollection GetTfaMechanisms()
        {
            var tfaRelay = ApplicationServiceContext.Current.GetService<ITfaRelayService>();
            if (tfaRelay == null)
                throw new InvalidOperationException("TFA Relay missing");
            return new AmiCollection()
            {
                CollectionItem = tfaRelay.Mechanisms.Select(o => new TfaMechanismInfo()
                {
                    Id = o.Id,
                    Name = o.Name
                }).OfType<Object>().ToList()
            };
        }

        /// <summary>
        /// Create a diagnostic report
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.Login)]
        public override DiagnosticReport CreateDiagnosticReport(DiagnosticReport report)
        {
            var persister = ApplicationServiceContext.Current.GetService<IDataPersistenceService<DiagnosticReport>>();
            if (persister == null)
                throw new InvalidOperationException("Cannot find appriopriate persister");
            return persister.Insert(report, TransactionMode.Commit, AuthenticationContext.Current.Principal);
        }

        /// <summary>
        /// Gets a specific log file.
        /// </summary>
        /// <param name="logId">The log identifier.</param>
        /// <returns>Returns the log file information.</returns>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public override LogFileInfo GetLog(string logId)
        {
            // Get the file trace writer
            var tracerPath = Tracer.GetWriter<RolloverTextWriterTraceWriter>()?.FileName;

            if (tracerPath == null)
            {
                throw new InvalidOperationException("No file-based trace writer was found");
            }

            var logFile = Path.Combine(Path.GetDirectoryName(tracerPath), logId + ".log");
            var retVal = new AmiCollection();
            var fi = new FileInfo(logFile);

            int offset = Int32.Parse(RestOperationContext.Current.IncomingRequest.QueryString["_offset"] ?? "0"),
                count = Int32.Parse(RestOperationContext.Current.IncomingRequest.QueryString["_count"] ?? "2048");

            if (offset > fi.Length || count > fi.Length) throw new ArgumentOutOfRangeException($"Log file {logId} is {fi.Length} but offset is greater at {offset}");
            using (var fs = File.OpenRead(logFile))
            {
                // Is count specified
                byte[] buffer;
                if (offset + count > fi.Length)
                    buffer = new byte[fi.Length - offset];
                else
                    buffer = new byte[count];

                fs.Seek(offset, SeekOrigin.Begin);
                fs.Read(buffer, 0, buffer.Length);

                return new LogFileInfo()
                {
                    Contents = buffer,
                    LastWrite = fi.LastWriteTime,
                    Name = fi.Name,
                    Size = fi.Length
                };
            }
        }

        /// <summary>
        /// Gets a specific log file.
        /// </summary>
        /// <param name="logId">The log identifier.</param>
        /// <returns>Returns the log file information.</returns>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public override Stream DownloadLog(string logId)
        {
            // Get the file trace writer
            var tracerPath = Tracer.GetWriter<RolloverTextWriterTraceWriter>()?.FileName;

            if (tracerPath == null)
            {
                throw new InvalidOperationException("No file-based trace writer was found");
            }

            var logFile = Path.Combine(Path.GetDirectoryName(tracerPath), logId + ".log");
            RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", $"attachment; filename={Path.GetFileName(logFile)}");
            RestOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            return File.OpenRead(logFile);
        }

        /// <summary>
        /// Get log files on the server and their sizes.
        /// </summary>
        /// <returns>Returns a collection of log files.</returns>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public override AmiCollection GetLogs()
        {
            // Get the file trace writer
            var tracerPath = Tracer.GetWriter<RolloverTextWriterTraceWriter>()?.FileName;

            if (tracerPath == null)
            {
                throw new InvalidOperationException("No file-based trace writer was found");
            }

            var logDirectory = Path.GetDirectoryName(tracerPath);
            var retVal = new AmiCollection();
            foreach (var itm in Directory.GetFiles(logDirectory, "*.log"))
            {
                var fi = new FileInfo(itm);
                retVal.CollectionItem.Add(new LogFileInfo()
                {
                    LastWrite = fi.LastWriteTime,
                    Name = Path.GetFileNameWithoutExtension(fi.Name),
                    Size = fi.Length
                });
            }
            return retVal;
        }

        /// <summary>
        /// Gets options for the AMI service.
        /// </summary>
        /// <returns>Returns options for the AMI service.</returns>
        public override ServiceOptions Options()
        {
            this.ThrowIfNotReady();
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.OK;
            RestOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(ApplicationServiceContext.Current.GetService<IPatchService>() != null ? ", PATCH" : null)}");

            if (ApplicationServiceContext.Current.GetService<IPatchService>() != null)
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+sdb-patch");

            // mex configuration
            var mexConfig = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            String boundHostPort = $"{RestOperationContext.Current.IncomingRequest.Url.Scheme}://{RestOperationContext.Current.IncomingRequest.Url.Host}:{RestOperationContext.Current.IncomingRequest.Url.Port}";
            if (!String.IsNullOrEmpty(mexConfig.ExternalHostPort))
            {
                var tUrl = new Uri(mexConfig.ExternalHostPort);
                boundHostPort = $"{tUrl.Scheme}://{tUrl.Host}:{tUrl.Port}";
            }

            var serviceOptions = new ServiceOptions
            {
                InterfaceVersion = "1.0.0.0",
                Endpoints = ApplicationServiceContext.Current.GetService<IServiceManager>().GetServices().OfType<IApiEndpointProvider>().Select(o =>
                    new ServiceEndpointOptions(o)
                    {
                        BaseUrl = o.Url.Select(url =>
                        {
                            var turi = new Uri(url);
                            return $"{boundHostPort}{turi.AbsolutePath}";
                        }).ToArray()
                    }
                ).ToList()
            };

            // Get endpoints
            var config = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AmiConfigurationSection>();

            if (config?.Endpoints != null)
                serviceOptions.Endpoints.AddRange(config.Endpoints);

            // Get the resources which are supported
            foreach (var itm in this.GetResourceHandler().Handlers)
            {
                var svc = this.ResourceOptions(itm.ResourceName);
                serviceOptions.Resources.Add(svc);
            }

            return serviceOptions;
        }

        /// <summary>
        /// Gets a server diagnostic report.
        /// </summary>
        /// <returns>Returns the created diagnostic report.</returns>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public override DiagnosticReport GetServerDiagnosticReport()
        {
            var retVal = new DiagnosticReport();
            retVal.ApplicationInfo = new DiagnosticApplicationInfo(Assembly.GetEntryAssembly());
            retVal.CreatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid);
            retVal.Threads = new List<DiagnosticThreadInfo>();

            try
            {
                if (Process.GetCurrentProcess()?.Threads == null)
                    this.m_traceSource.TraceEvent(EventLevel.Warning, "Threading information is not available for this instance");
                else
                    foreach (ProcessThread thd in Process.GetCurrentProcess().Threads.OfType<ProcessThread>().Where(o => o != null))
                        retVal.Threads.Add(new DiagnosticThreadInfo()
                        {
                            Name = thd.Id.ToString(),
                            CpuTime = thd.UserProcessorTime,
                            WaitReason = null,
                            State = thd.ThreadState.ToString(),
                            TaskInfo = thd.ThreadState == ThreadState.Wait ? thd.WaitReason.ToString() : "N/A"
                        });
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, "Error gathering thread information: {0}", e);
            }

            retVal.ApplicationInfo.Assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(o => new DiagnosticVersionInfo(o)).ToList();
            retVal.ApplicationInfo.EnvironmentInfo = new DiagnosticEnvironmentInfo()
            {
                Is64Bit = Environment.Is64BitOperatingSystem && Environment.Is64BitProcess,
                OSVersion = String.Format("{0} v{1}", System.Environment.OSVersion.Platform, System.Environment.OSVersion.Version),
                ProcessorCount = Environment.ProcessorCount,
                UsedMemory = GC.GetTotalMemory(false),
                Version = Environment.Version.ToString(),
            };
            retVal.ApplicationInfo.Applets = ApplicationServiceContext.Current.GetService<IAppletManagerService>().Applets.Select(o => o.Info).ToList();
            retVal.ApplicationInfo.ServiceInfo = ApplicationServiceContext.Current.GetService<IServiceManager>().GetAllTypes().Where(o => o.GetInterfaces().Any(i => i.FullName == typeof(IServiceImplementation).FullName) && !o.IsGenericTypeDefinition && !o.IsAbstract && !o.IsInterface).Select(o => new DiagnosticServiceInfo(o)).ToList();
            return retVal;
        }

        /// <summary>
        /// Creates the specified resource for the AMI service
        /// </summary>
        /// <param name="resourceType">The type of resource being created</param>
        /// <param name="data">The resource data being created</param>
        /// <returns>The created the data</returns>
        public override Object Create(string resourceType, Object data)
        {
            return base.Create(resourceType, data);
        }

        /// <summary>
        /// Create or update the specific resource
        /// </summary>
        public override Object CreateUpdate(string resourceType, string key, Object data)
        {
            return base.CreateUpdate(resourceType, key, data);
        }

        /// <summary>
        /// Delete the specified resource
        /// </summary>
        public override Object Delete(string resourceType, string key)
        {
            return base.Delete(resourceType, key);
        }

        /// <summary>
        /// Get the specified resource
        /// </summary>
        public override Object Get(string resourceType, string key)
        {
            return base.Get(resourceType, key);
        }

        /// <summary>
        /// Get a specific version of the resource
        /// </summary>
        public override Object GetVersion(string resourceType, string key, string versionKey)
        {
            return base.GetVersion(resourceType, key, versionKey);
        }

        /// <summary>
        /// Get the complete history of a resource
        /// </summary>
        public override AmiCollection History(string resourceType, string key)
        {
            return base.History(resourceType, key);
        }

        /// <summary>
        /// Get options / capabilities of a specific object
        /// </summary>
        public override ServiceResourceOptions ResourceOptions(string resourceType)
        {
            return base.ResourceOptions(resourceType);
        }

        /// <summary>
        /// Performs a search of the specified AMI resource
        /// </summary>
        public override AmiCollection Search(string resourceType)
        {
            return base.Search(resourceType);
        }

        /// <summary>
        /// Updates the specified object on the server
        /// </summary>
        public override Object Update(string resourceType, string key, Object data)
        {
            return base.Update(resourceType, key, data);
        }

        /// <summary>
        /// Perform a head operation
        /// </summary>
        /// <param name="resourceType">The resource type</param>
        /// <param name="id">The id of the resource</param>
        public override void Head(string resourceType, string id)
        {
            base.Head(resourceType, id);
        }

        /// <summary>
        /// Service is not ready
        /// </summary>
        protected override void ThrowIfNotReady()
        {
            if (!ApplicationServiceContext.Current.IsRunning)
                throw new DomainStateException();
        }

        /// <summary>
        /// Lock resource
        /// </summary>
        public override object Lock(string resourceType, string key)
        {
            return base.Lock(resourceType, key);
        }

        /// <summary>
        /// Unlock resource
        /// </summary>
        public override object UnLock(string resourceType, string key)
        {
            return base.UnLock(resourceType, key);
        }
    }
}