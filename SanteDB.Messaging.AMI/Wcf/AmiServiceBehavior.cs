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
using System.ServiceModel.Web;
using System.Net;
using System.ServiceModel.Channels;
using SanteDB.Messaging.Common;
using SanteDB.Core.Model;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Model.AMI.Diagnostics;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Security;
using System.Security.Permissions;
using SanteDB.Core.Model.AMI.Logging;
using System.Reflection;
using SanteDB.Core.Model.AMI.Security;
using SwaggerWcf.Attributes;
using System.Xml.Serialization;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Services;
using SanteDB.Core.Wcf;
using SanteDB.Messaging.AMI.Configuration;
using MARC.HI.EHRS.SVC.Core.Services.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.AMI.Collections;
using MARC.Everest.Threading;
using SanteDB.Core.Model.AMI;

namespace SanteDB.Messaging.AMI.Wcf
{
    /// <summary>
    /// Implementation of the AMI service behavior
    /// </summary>
    [ServiceBehavior(ConfigurationName = "AMI", InstanceContextMode = InstanceContextMode.PerCall)]
    [Description("Administrative Management Interface")]
    [TraceSource(AmiConstants.TraceSourceName)]
    public class AmiServiceBehavior : IAmiServiceContract
    {

        // The trace source for logging
        private TraceSource m_traceSource = new TraceSource(AmiConstants.TraceSourceName);

        /// <summary>
        /// Create a diagnostic report
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.Login)]
        public DiagnosticReport CreateDiagnosticReport(DiagnosticReport report)
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
        public LogFileInfo GetLog(string logId)
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
        public AmiCollection GetLogs()
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
        /// Gets the schema for the administrative interface.
        /// </summary>
        /// <param name="schemaId">The id of the schema to be retrieved.</param>
        /// <returns>Returns the administrative interface schema.</returns>
        [SwaggerWcfHidden]
        public XmlSchema GetSchema(int schemaId)
        {
            try
            {
                XmlSchemas schemaCollection = new XmlSchemas();

                XmlReflectionImporter importer = new XmlReflectionImporter("http://santedb.org/ami");
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemaCollection);

                foreach (var cls in typeof(IAmiServiceContract).GetCustomAttributes<ServiceKnownTypeAttribute>().Select(o => o.Type))
                    exporter.ExportTypeMapping(importer.ImportTypeMapping(cls, "http://santedb.org/ami"));

                if (schemaId > schemaCollection.Count)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotFound;
                    return null;
                }
                else
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.OK;
                    WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
                    return schemaCollection[schemaId];
                }
            }
            catch (Exception e)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets a server diagnostic report.
        /// </summary>
        /// <returns>Returns the created diagnostic report.</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedAdministration)]
        public DiagnosticReport GetServerDiagnosticReport()
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
        public AmiCollection GetTfaMechanisms()
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
        /// Gets options for the AMI service.
        /// </summary>
        /// <returns>Returns options for the AMI service.</returns>
        public ServiceOptions Options()
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Allow", $"GET, PUT, POST, OPTIONS, HEAD, DELETE{(ApplicationContext.Current.GetService<IPatchService>() != null ? ", PATCH" : null)}");

            if (ApplicationContext.Current.GetService<IPatchService>() != null)
            {
                WebOperationContext.Current.OutgoingResponse.Headers.Add("Accept-Patch", "application/xml+oiz-patch");
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
        /// Perform a ping
        /// </summary>
        [SwaggerWcfHidden]
        public void Ping()
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.NoContent;
        }

        /// <summary>
        /// Creates security reset information
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public void SendTfaSecret(TfaRequestInfo resetInfo)
        {
            var securityRepository = ApplicationContext.Current.GetService<ISecurityRepositoryService>();

            var securityUser = securityRepository.GetUser(resetInfo.UserName);

            // don't throw an error if the user is not found, just act as if we sent it.
            // this is to make sure that people cannot guess users
            if (securityUser == null)
            {
                this.m_traceSource.TraceEvent(TraceEventType.Warning, 0, "Attempt to get TFA reset code for {0} which is not a valid user", resetInfo.UserName);
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
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
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
        }
        /// <summary>
        /// Creates the specified resource for the AMI service 
        /// </summary>
        /// <param name="resourceType">The type of resource being created</param>
        /// <param name="data">The resource data being created</param>
        /// <returns>The created the data</returns>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public Object Create(string resourceType, Object data)
        {
            this.ThrowIfNotReady();

            try
            {

                IResourceHandler handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    var retVal = handler.Create(data, false);

                    var versioned = retVal as IVersionedEntity;
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Created;
                    WebOperationContext.Current.OutgoingResponse.ETag = (retVal as IdentifiedData).Tag;
                    if (versioned != null)
                        WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                           WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri,
                           resourceType,
                           (retVal as IdentifiedData).Key,
                           versioned.Key));
                    else
                        WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri,
                            resourceType,
                            (retVal as IdentifiedData).Key));
                }
                else
                    throw new FileNotFoundException(resourceType);
                return null;
            }
            catch (Exception e)
            {
                var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Create or update the specific resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public Object CreateUpdate(string resourceType, string key, Object data)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    var retVal = handler.Create(data, true) as IdentifiedData;
                    var versioned = retVal as IVersionedEntity;
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Created;
                    WebOperationContext.Current.OutgoingResponse.ETag = retVal.Tag;

                    if (versioned != null)
                        WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri,
                            resourceType,
                            retVal.Key,
                            versioned.Key));
                    else
                        WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri,
                            resourceType,
                            retVal.Key));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Delete the specified resource
        /// </summary>

        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public Object Delete(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {

                    var retVal = handler.Obsolete(Guid.Parse(key)) as IdentifiedData;

                    var versioned = retVal as IVersionedEntity;

                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.Created;
                    if (versioned != null)
                        WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri,
                            resourceType,
                            retVal.Key,
                            versioned.Key));
                    else
                        WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri,
                            resourceType,
                            retVal.Key));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Get the specified resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public Object Get(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {


                var handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    var retVal = handler.Get(Guid.Parse(key), Guid.Empty) as IdentifiedData;
                    if (retVal == null)
                        throw new FileNotFoundException(key);

                    WebOperationContext.Current.OutgoingResponse.ETag = retVal.Tag;
                    WebOperationContext.Current.OutgoingResponse.LastModified = retVal.ModifiedOn.DateTime;

                    // HTTP IF headers?
                    if (WebOperationContext.Current.IncomingRequest.IfModifiedSince.HasValue &&
                        retVal.ModifiedOn <= WebOperationContext.Current.IncomingRequest.IfModifiedSince ||
                        WebOperationContext.Current.IncomingRequest.IfNoneMatch?.Any(o => retVal.Tag == o) == true)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotModified;
                        return null;
                    }
                    else
                    {
                        return retVal;
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Get a specific version of the resource 
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public Object GetVersion(string resourceType, string key, string versionKey)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    var retVal = handler.Get(Guid.Parse(key), Guid.Parse(versionKey)) as IdentifiedData;
                    if (retVal == null)
                        throw new FileNotFoundException(key);


                    WebOperationContext.Current.OutgoingResponse.ETag = retVal.Tag;
                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Get the complete history of a resource 
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public AmiCollection History(string resourceType, string key)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);

                if (handler != null)
                {
                    String since = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["_since"];
                    Guid sinceGuid = since != null ? Guid.Parse(since) : Guid.Empty;

                    // Query 
                    var retVal = handler.Get(Guid.Parse(key), Guid.Empty) as IVersionedEntity;
                    List<IVersionedEntity> histItm = new List<IVersionedEntity>() { retVal };
                    while (retVal.PreviousVersionKey.HasValue)
                    {
                        retVal = handler.Get(Guid.Parse(key), retVal.PreviousVersionKey.Value) as IVersionedEntity;
                        if (retVal != null)
                            histItm.Add(retVal);
                        // Should we stop fetching?
                        if (retVal?.VersionKey == sinceGuid)
                            break;

                    }

                    // Lock the item
                    return new AmiCollection(histItm, 0, histItm.Count);
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Get options / capabilities of a specific object
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public ServiceResourceOptions ResourceOptions(string resourceType)
        {
            var handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);
            if (handler == null)
                throw new FileNotFoundException(resourceType);
            else
                return new ServiceResourceOptions(resourceType, handler.Capabilities);
        }

        /// <summary>
        /// Performs a search of the specified AMI resource
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public AmiCollection Search(string resourceType)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {
                    String offset = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["_offset"],
                        count = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["_count"];

                    var query = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters.ToQuery();

                    // Modified on?
                    if (WebOperationContext.Current.IncomingRequest.IfModifiedSince.HasValue)
                        query.Add("modifiedOn", ">" + WebOperationContext.Current.IncomingRequest.IfModifiedSince.Value.ToString("o"));

                    // No obsoletion time?
                    if (typeof(BaseEntityData).IsAssignableFrom(handler.Type) && !query.ContainsKey("obsoletionTime"))
                        query.Add("obsoletionTime", "null");

                    int totalResults = 0;

                    // Lean mode
                    var lean = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["_lean"];
                    bool parsedLean = false;
                    bool.TryParse(lean, out parsedLean);


                    var retVal = handler.Query(query, Int32.Parse(offset ?? "0"), Int32.Parse(count ?? "100"), out totalResults).ToList();
                    WebOperationContext.Current.OutgoingResponse.LastModified = retVal.OfType<IdentifiedData>().OrderByDescending(o => o.ModifiedOn).FirstOrDefault()?.ModifiedOn.DateTime ?? DateTime.Now;


                    // Last modification time and not modified conditions
                    if ((WebOperationContext.Current.IncomingRequest.IfModifiedSince.HasValue ||
                        WebOperationContext.Current.IncomingRequest.IfNoneMatch != null) &&
                        totalResults == 0)
                    {
                        WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NotModified;
                        return null;
                    }
                    else
                    {
                        return new AmiCollection(retVal, Int32.Parse(offset ?? "0"), totalResults);
                    }
                }
                else
                    throw new FileNotFoundException(resourceType);
            }
            catch (Exception e)
            {
                var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Updates the specified object on the server
        /// </summary>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public Object Update(string resourceType, string key, Object data)
        {
            this.ThrowIfNotReady();
            try
            {
                var handler = ResourceHandlerUtil.Current.GetResourceHandler<IAmiServiceContract>(resourceType);
                if (handler != null)
                {

                    var retVal = handler.Update(data);

                    var versioned = retVal as IVersionedEntity;
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.OK;
                    WebOperationContext.Current.OutgoingResponse.ETag = (retVal as IdentifiedData)?.Tag;

                    if (versioned != null)
                        WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}/history/{3}",
                            WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri,
                            resourceType,
                            (retVal as IIdentifiedEntity)?.Key?.ToString() ?? (retVal as IAmiIdentified)?.Key,
                            versioned.Key));
                    else
                        WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.ContentLocation, String.Format("{0}/{1}/{2}",
                            WebOperationContext.Current.IncomingRequest.UriTemplateMatch.BaseUri,
                            resourceType,
                            (retVal as IIdentifiedEntity)?.Key?.ToString() ?? (retVal as IAmiIdentified)?.Key));

                    return retVal;
                }
                else
                    throw new FileNotFoundException(resourceType);

            }
            catch (Exception e)
            {
                var remoteEndpoint = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                this.m_traceSource.TraceEvent(TraceEventType.Error, e.HResult, String.Format("{0} - {1}", remoteEndpoint?.Address, e.ToString()));
                throw;

            }
        }

        /// <summary>
        /// Perform a head operation
        /// </summary>
        /// <param name="resourceType">The resource type</param>
        /// <param name="id">The id of the resource</param>
        [PolicyPermission(SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.LoginAsService)]
        public void Head(string resourceType, string id)
        {
            this.ThrowIfNotReady();
            this.Get(resourceType, id);
        }

        /// <summary>
        /// Service is not ready
        /// </summary>
        [SwaggerWcfHidden]
        private void ThrowIfNotReady()
        {
            if (!ApplicationContext.Current.IsRunning)
                throw new DomainStateException();
        }
    }
}
