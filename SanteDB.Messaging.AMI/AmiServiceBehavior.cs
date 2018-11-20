using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Patch;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core.Attributes;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Exceptions;
using SanteDB.Core.Diagnostics;
using System.IO;
using SanteDB.Core.Model.Interfaces;
using System.Net;
using System.ServiceModel.Channels;
using SanteDB.Rest.Common;
using SanteDB.Core.Model;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Model.AMI.Diagnostics;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Security;
using System.Security.Permissions;
using SanteDB.Core.Model.AMI.Logging;
using System.Reflection;
using SanteDB.Core.Model.AMI.Security;
using System.Xml.Serialization;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Services;
using SanteDB.Messaging.AMI.Configuration;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.AMI.Collections;
using MARC.Everest.Threading;
using SanteDB.Core.Model.AMI;
using RestSrvr.Attributes;
using RestSrvr;
using SanteDB.Core.Rest;
using SanteDB.Rest.AMI;

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
            var persister = ApplicationContext.Current.GetService<IDataPersistenceService<DiagnosticReport>>();
            if (persister == null)
                throw new InvalidOperationException("Cannot find appriopriate persister");
            return persister.Insert(report, AuthenticationContext.Current.Principal, TransactionMode.Commit);
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
            RestOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(ApplicationContext.Current.GetService<IPatchService>() != null ? ", PATCH" : null)}");

            if (ApplicationContext.Current.GetService<IPatchService>() != null)
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
                Endpoints = ApplicationContext.Current.GetServices().OfType<IApiEndpointProvider>().Select(o =>
                    new ServiceEndpointOptions
                    {
                        BaseUrl = o.Url,
                        ServiceType = o.ApiType,
                        Capabilities = o.Capabilities
                    }
                ).ToList()
            };

            // Get endpoints

            var config = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("santedb.messaging.ami") as AmiConfiguration;

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
                    State = thd.ThreadState.ToString()
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
            retVal.ApplicationInfo.ServiceInfo = ApplicationContext.Current.GetServices().OfType<IDaemonService>().Select(o => new DiagnosticServiceInfo(o)).ToList();
            return retVal;
        }

        /// <summary>
        /// Get a list of TFA mechanisms
        /// </summary>
        /// <returns>Returns a list of TFA mechanisms.</returns>
        public override AmiCollection GetTfaMechanisms()
        {
            var tfaRelay = ApplicationContext.Current.GetService<ITfaRelayService>();
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
            var securityRepository = ApplicationContext.Current.GetService<ISecurityRepositoryService>();

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
            var identityProvider = ApplicationContext.Current.GetService<IIdentityProviderService>();
            var tfaSecret = identityProvider.GenerateTfaSecret(securityUser.UserName);

            // Add a claim
            if (resetInfo.Purpose == "PasswordReset")
            {
                new PolicyPermission(PermissionState.Unrestricted, PermissionPolicyIdentifiers.LoginAsService);
                identityProvider.AddClaim(securityUser.UserName, new System.Security.Claims.Claim(SanteDBClaimTypes.SanteDBPasswordlessAuth, "true"));
            }

            var tfaRelay = ApplicationContext.Current.GetService<ITfaRelayService>();
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
            if (!ApplicationContext.Current.IsRunning)
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
    }
}
