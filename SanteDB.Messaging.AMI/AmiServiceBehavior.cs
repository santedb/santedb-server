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
using Microsoft.Diagnostics.Runtime;
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Rest;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Messaging.AMI.Configuration;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Permissions;

namespace SanteDB.Messaging.AMI.Wcf
{
    /// <summary>
    /// Implementation of the AMI service behavior
    /// </summary>
    public class AmiServiceBehavior : AmiServiceBehaviorBase
    {

        // The trace source for logging
        private TraceSource m_traceSource = new TraceSource(AmiConstants.TraceSourceName);

        /// <summary>
        /// Create a new ami service behavior
        /// </summary>
        public AmiServiceBehavior() : base(AmiMessageHandler.ResourceHandler)
        {

        }

        /// <summary>
        /// Create a diagnostic report
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.Login)]
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
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public override LogFileInfo GetLog(string logId)
        {
            var logFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), logId + ".log");
            var retVal = new AmiCollection();
            var fi = new FileInfo(logFile);
            return new LogFileInfo()
            {
                LastWrite = fi.LastWriteTime,
                Name = fi.Name,
                Size = fi.Length,
                Contents = File.ReadAllBytes(logFile)
            };
        }

        /// <summary>
        /// Get log files on the server and their sizes.
        /// </summary>
        /// <returns>Returns a collection of log files.</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public override AmiCollection GetLogs()
        {
            var logDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
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
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.OK;
            RestOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(ApplicationServiceContext.Current.GetService<IPatchService>() != null ? ", PATCH" : null)}");

            if (ApplicationServiceContext.Current.GetService<IPatchService>() != null)
            {
                RestOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+oiz-patch");
            }

            var serviceOptions = new ServiceOptions
            {
                InterfaceVersion = "1.0.0.0",
                Services = new List<ServiceResourceOptions>
                {
                    new ServiceResourceOptions
                    {
                        ResourceName = "time",
                        Capabilities = ResourceCapability.Get
                    }
                },
                Endpoints = (ApplicationServiceContext.Current as IServiceManager).GetServices().OfType<IApiEndpointProvider>().Select(o =>
                    new ServiceEndpointOptions
                    {
                        BaseUrl = o.Url,
                        ServiceType = o.ApiType,
                        Capabilities = o.Capabilities
                    }
                ).ToList()
            };

            // Get endpoints

            var config = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AmiConfigurationSection>();

            if (config?.Endpoints != null)
                serviceOptions.Endpoints.AddRange(config.Endpoints);

            return serviceOptions;
        }

        /// <summary>
        /// Gets a server diagnostic report.
        /// </summary>
        /// <returns>Returns the created diagnostic report.</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public override DiagnosticReport GetServerDiagnosticReport()
        {
            var retVal = new DiagnosticReport();
            retVal.ApplicationInfo = new DiagnosticApplicationInfo(Assembly.GetEntryAssembly());
            retVal.CreatedByKey = Guid.Parse(AuthenticationContext.SystemUserSid);
            retVal.Threads = new List<DiagnosticThreadInfo>();

            foreach (ProcessThread thd in Process.GetCurrentProcess().Threads)
                retVal.Threads.Add(new DiagnosticThreadInfo()
                {
                    Name = thd.Id.ToString(),
                    CpuTime = thd.UserProcessorTime,
                    WaitReason = null,
                    State = thd.ThreadState.ToString(),
                    TaskInfo = thd.ThreadState == ThreadState.Wait ? thd.WaitReason.ToString() : "N/A"
                });

            retVal.ApplicationInfo.Assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(o => new DiagnosticVersionInfo(o)).ToList();
            retVal.ApplicationInfo.EnvironmentInfo = new DiagnosticEnvironmentInfo()
            {
                Is64Bit = Environment.Is64BitOperatingSystem && Environment.Is64BitProcess,
                OSVersion = String.Format("{0} v{1}", System.Environment.OSVersion.Platform, System.Environment.OSVersion.Version),
                ProcessorCount = Environment.ProcessorCount,
                UsedMemory = GC.GetTotalMemory(false),
                Version = Environment.Version.ToString(),
            };
            retVal.ApplicationInfo.ServiceInfo = (ApplicationServiceContext.Current as IServiceManager).GetServices().Distinct().Select(o => new DiagnosticServiceInfo(o)).ToList();
            return retVal;
        }

        /// <summary>
        /// Get a list of TFA mechanisms
        /// </summary>
        /// <returns>Returns a list of TFA mechanisms.</returns>
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
                    Name = o.Name,
                    ChallengeText = o.Challenge
                }).OfType<Object>().ToList()
            };
        }

        /// <summary>
        /// Creates security reset information
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override void SendTfaSecret(TfaRequestInfo resetInfo)
        {
            var securityRepository = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();

            var securityUser = securityRepository.GetUser(resetInfo.UserName);

            // don't throw an error if the user is not found, just act as if we sent it.
            // this is to make sure that people cannot guess users
            if (securityUser == null)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Warning, 0, "Attempt to get TFA reset code for {0} which is not a valid user", resetInfo.UserName);
                RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            // Identity provider
            var identityProvider = ApplicationServiceContext.Current.GetService<IIdentityProviderService>();
            var tfaSecret = identityProvider.GenerateTfaSecret(securityUser.UserName);

            // Add a claim
            if (resetInfo.Purpose == "PasswordReset")
            {
                new PolicyPermission(PermissionState.Unrestricted, PermissionPolicyIdentifiers.LoginAsService);
                identityProvider.AddClaim(securityUser.UserName, new GenericClaim(SanteDBClaimTypes.SanteDBPasswordlessAuth, "true"), AuthenticationContext.SystemPrincipal);
            }

            var tfaRelay = ApplicationServiceContext.Current.GetService<ITfaRelayService>();
            if (tfaRelay == null)
                throw new InvalidOperationException("TFA relay not specified");

            // Now issue the TFA secret
            tfaRelay.SendSecret(resetInfo.ResetMechanism, securityUser, resetInfo.Verification, tfaSecret);
            RestOperationContext.Current.OutgoingResponse.StatusCode = (int)HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Creates the specified resource for the AMI service 
        /// </summary>
        /// <param name="resourceType">The type of resource being created</param>
        /// <param name="data">The resource data being created</param>
        /// <returns>The created the data</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override Object Create(string resourceType, Object data)
        {
            return base.Create(resourceType, data);
        }

        /// <summary>
        /// Create or update the specific resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override Object CreateUpdate(string resourceType, string key, Object data)
        {
            return base.CreateUpdate(resourceType, key, data);
        }

        /// <summary>
        /// Delete the specified resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override Object Delete(string resourceType, string key)
        {
            return base.Delete(resourceType, key);
        }

        /// <summary>
        /// Get the specified resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override Object Get(string resourceType, string key)
        {
            return base.Get(resourceType, key);
        }

        /// <summary>
        /// Get a specific version of the resource 
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override Object GetVersion(string resourceType, string key, string versionKey)
        {
            return base.GetVersion(resourceType, key, versionKey);
        }

        /// <summary>
        /// Get the complete history of a resource 
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override AmiCollection History(string resourceType, string key)
        {
            return base.History(resourceType, key);
        }

        /// <summary>
        /// Get options / capabilities of a specific object
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override ServiceResourceOptions ResourceOptions(string resourceType)
        {
            return base.ResourceOptions(resourceType);
        }

        /// <summary>
        /// Performs a search of the specified AMI resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override AmiCollection Search(string resourceType)
        {
            return base.Search(resourceType);
        }

        /// <summary>
        /// Updates the specified object on the server
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override Object Update(string resourceType, string key, Object data)
        {
            return base.Update(resourceType, key, data);
        }

        /// <summary>
        /// Perform a head operation
        /// </summary>
        /// <param name="resourceType">The resource type</param>
        /// <param name="id">The id of the resource</param>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
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
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override object Lock(string resourceType, string key)
        {
            return base.Lock(resourceType, key);
        }

        /// <summary>
        /// Unlock resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public override object UnLock(string resourceType, string key)
        {
            return base.UnLock(resourceType, key);
        }

        /// <summary>
        /// Demand permission
        /// </summary>
        /// <param name="policyId"></param>
        protected override void Demand(string policyId)
        {
            new PolicyPermission(PermissionState.Unrestricted, policyId).Demand();
        }
    }
}
