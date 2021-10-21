/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using SanteDB.Core;
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Server.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents an applet manager service that uses the local file system
    /// </summary>
    [ServiceProvider("Local Applet Repository/Manager", Configuration = typeof(AppletConfigurationSection))]
    public class LocalAppletManagerService : IAppletManagerService, IAppletSolutionManagerService, IDaemonService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Applet Repository/Manager";

        // Solutions registered
        private ObservableCollection<AppletSolution> m_solutions = new ObservableCollection<AppletSolution>();

        // Applet collection
        private Dictionary<String, AppletCollection> m_appletCollection = new Dictionary<string, AppletCollection>();

        // RO applet collection
        private Dictionary<String, ReadonlyAppletCollection> m_readonlyAppletCollection = new Dictionary<string, ReadonlyAppletCollection>();

        // Map of package id to file
        private Dictionary<String, String> m_fileDictionary = new Dictionary<string, string>();

        // Config file
        private AppletConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AppletConfigurationSection>();

        // Tracer
        private Tracer m_tracer = new Tracer(SanteDBConstants.ServiceTraceSourceName + ".AppletManager");

        // Localizaiton Service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// Indicates whether the service is running
        /// </summary>
        public bool IsRunning => true;

        /// <summary>
        /// Local applet manager ctor
        /// </summary>
        public LocalAppletManagerService(ILocalizationService localizationService)
        {
            this.m_appletCollection.Add(String.Empty, new AppletCollection()); // Default applet
            this.m_readonlyAppletCollection.Add(String.Empty, this.m_appletCollection[String.Empty].AsReadonly());
            this.m_readonlyAppletCollection.First().Value.CollectionChanged += (o, e) => this.Changed?.Invoke(o, e);
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Gets the loaded applets from the manager
        /// </summary>
        public ReadonlyAppletCollection Applets
        {
            get
            {
                return this.m_readonlyAppletCollection[String.Empty];
            }
        }

        /// <summary>
        /// Get the solutions
        /// </summary>
        public IEnumerable<AppletSolution> Solutions => this.m_solutions;

        /// <summary>
        /// The daemon has started
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// The daemon is starting
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// The daemon has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// The daemon is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Applet has changed
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Get the specified applet
        /// </summary>
        public byte[] GetPackage(String appletId)
        {
            return this.GetPackage(String.Empty, appletId);
        }

        /// <summary>
        /// Get the specified package data
        /// </summary>
        public byte[] GetPackage(String solutionId, String appletId)
        {
            this.m_tracer.TraceInfo("Retrieving package {0}", appletId);

            // Save the applet
            var appletDir = Path.Combine(this.m_configuration.AppletDirectory, solutionId);
            if (!Directory.Exists(appletDir))
                Directory.CreateDirectory(appletDir);

            // Install
            String pakFile = null;
            if (this.m_fileDictionary.TryGetValue($"{solutionId}{appletId}", out pakFile) && File.Exists(pakFile))
                return File.ReadAllBytes(pakFile);
            else
            {
                this.m_tracer.TraceError($"Applet {appletId} not found");
                throw new FileNotFoundException(this.m_localizationService.FormatString("error.server.core.applet.notFound", new
                {
                    param = appletId
                }));
            }
        }

        /// <summary>
        /// Uninstall the applet package
        /// </summary>
        public bool UnInstall(String packageId)
        {
            this.m_tracer.TraceInfo("Un-installing {0}", packageId);
            // Applet check
            var applet = this.m_appletCollection[String.Empty].FirstOrDefault(o => o.Info.Id == packageId);
            if (applet == null) // Might be solution
            {
                var soln = this.m_solutions.FirstOrDefault(o => o.Meta.Id == packageId);
                if (soln == null)
                {
                    this.m_tracer.TraceError($"Applet {packageId} is not installed");
                    throw new FileNotFoundException(this.m_localizationService.FormatString("error.server.core.applet.notInstalled", new
                    {
                        param = packageId
                    }));
                }
                else
                {
                    this.m_solutions.Remove(soln);
                    lock (this.m_fileDictionary)
                        if (this.m_fileDictionary.ContainsKey(packageId + ".sln"))
                            File.Delete(this.m_fileDictionary[packageId + ".sln"]);
                }
            }
            else
            {
                // Dependency check
                var dependencies = this.m_appletCollection[String.Empty].Where(o => o.Info.Dependencies.Any(d => d.Id == packageId));
                if (dependencies.Any())
                {
                    this.m_tracer.TraceError($"Uninstalling {packageId} would break : {string.Join(", ", dependencies.Select(o => o.Info))}");
                    throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.uninstallingBreak", new
                    {
                        param = packageId,
                        param2 = string.Join(", ", dependencies.Select(o => o.Info))
                    }));
                }
                // We're good to go!
                this.m_appletCollection[String.Empty].Remove(applet);

                lock (this.m_fileDictionary)
                    if (this.m_fileDictionary.ContainsKey(packageId))
                        File.Delete(this.m_fileDictionary[packageId]);

                AppletCollection.ClearCaches();
            }
            return true;
        }

        /// <summary>
        /// Install package
        /// </summary>
        public bool Install(AppletPackage package, bool isUpgrade = false)
        {
            return this.Install(package, isUpgrade, null);
        }

        /// <summary>
        /// Performs an installation
        /// </summary>
        public bool Install(AppletPackage package, bool isUpgrade, AppletSolution owner)
        {
            this.m_tracer.TraceInfo("Installing {0}", package.Meta);

            var appletScope = owner?.Meta.Id ?? String.Empty;
            // TODO: Verify package hash / signature
            if (!this.VerifyPackage(package))
            {
                this.m_tracer.TraceError("Applet failed validation");
                throw new SecurityException(this.m_localizationService.GetString("error.server.core.applet.failedValidation"));

            }
            else if (!this.m_appletCollection[appletScope].VerifyDependencies(package.Meta))
            {
                this.m_tracer.TraceWarning($"Applet {package.Meta} depends on : [{String.Join(", ", package.Meta.Dependencies.Select(o => o.ToString()))}] which are missing or incompatible");
            }

            // Save the applet
            var appletDir = this.m_configuration.AppletDirectory;
            if (!Path.IsPathRooted(appletDir))
                appletDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), this.m_configuration.AppletDirectory);
            if (owner != null)
                appletDir = Path.Combine(appletDir, owner.Meta.Id);
            if (!Directory.Exists(appletDir))
                Directory.CreateDirectory(appletDir);

            // Install
            var pakFile = Path.Combine(appletDir, package.Meta.Id + ".pak");
            if (this.m_appletCollection[appletScope].Any(o => o.Info.Id == package.Meta.Id) && File.Exists(pakFile) && !isUpgrade)
            {
                this.m_tracer.TraceError($"Cannot replace {package.Meta} unless upgrade is specifically specified");
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.upgrade", new
                {
                    param = package.Meta
                }));
            }

            using (var fs = File.Create(pakFile))
            {
                package.Save(fs);
            }

            lock (this.m_fileDictionary)
                if (!this.m_fileDictionary.ContainsKey($"{appletScope}{package.Meta.Id}"))
                    this.m_fileDictionary.Add($"{appletScope}{package.Meta.Id}", pakFile);

            var pkg = package.Unpack();

            // remove the package from the collection if this is an upgrade
            if (isUpgrade)
            {
                this.m_appletCollection[appletScope].Remove(pkg);
            }

            this.m_appletCollection[appletScope].Add(pkg);

            // We want to install the templates & protocols into the DB
            this.m_tracer.TraceInfo("Installing templates...");

            // Install templates
            var idp = ApplicationServiceContext.Current.GetService<ITemplateDefinitionRepositoryService>();
            if (idp != null)
                foreach (var itm in pkg.Templates)
                {
                    if (idp.GetTemplateDefinition(itm.Mnemonic) == null)
                    {
                        this.m_tracer.TraceInfo("Installing {0}...", itm.Mnemonic);
                        idp.Insert(new TemplateDefinition()
                        {
                            Oid = itm.Oid,
                            Mnemonic = itm.Mnemonic,
                            Description = itm.Description,
                            Name = itm.Mnemonic
                        });
                    }
                }

            AppletCollection.ClearCaches();

            return true;
        }

        /// <summary>
        /// Verify package signature
        /// </summary>
        private bool VerifyPackage(AppletPackage package)
        {
            byte[] verifyBytes = package.Manifest;
            // First check: Hash - Make sure the HASH is ok
            if (package is AppletSolution)
            {
                verifyBytes = (package as AppletSolution).Include.SelectMany(o => o.Manifest).ToArray();
                if (BitConverter.ToString(SHA256.Create().ComputeHash(verifyBytes)) != BitConverter.ToString(package.Meta.Hash))
                {
                    this.m_tracer.TraceError($"Package contents of {package.Meta.Id} appear to be corrupt!");
                    throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.contentsCorrupted", new
                    {
                        param = package.Meta.Id
                    }));
                }
            }
            else if (BitConverter.ToString(SHA256.Create().ComputeHash(package.Manifest)) != BitConverter.ToString(package.Meta.Hash))
            {
                this.m_tracer.TraceError($"Package contents of {package.Meta.Id} appear to be corrupt!");
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.contentsCorrupted", new
                {
                    param = package.Meta.Id
                }));
            }

            if (package.Meta.Signature != null)
            {
                this.m_tracer.TraceInfo("Will verify package {0}", package.Meta.Id.ToString());

                // Get the public key
                var x509Store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                try
                {
                    x509Store.Open(OpenFlags.ReadOnly);
                    var cert = x509Store.Certificates.Find(X509FindType.FindByThumbprint, package.Meta.PublicKeyToken, false);

                    if (cert.Count == 0)
                    {
                        if (package.PublicKey != null)
                        {
                            // Embedded cert and trusted CA
                            X509Certificate2 embCert = new X509Certificate2(package.PublicKey);
                            if (!embCert.IsTrustedIntern(new X509Certificate2Collection(), out IEnumerable<X509ChainStatus> chainStatus))
                            { 
                                this.m_tracer.TraceError($"Cannot verify identity of publisher {embCert.Subject} - {string.Join(",", chainStatus.Select(o => o.Status))}");
                                throw new SecurityException(this.m_localizationService.FormatString("error.server.core.publisherIdentity", new
                                {
                                    param = embCert.Subject,
                                    param2 = string.Join(",", chainStatus.Select(o => o.Status))
                                }));
                            }
                            else
                                cert = new X509Certificate2Collection(embCert);
                        }
                        else
                        {
                           this.m_tracer.TraceError($"Cannot find public key of publisher information for {package.Meta.PublicKeyToken} or the local certificate is invalid");
                            throw new SecurityException(this.m_localizationService.FormatString("error.server.core.publicKey", new
                            {
                                param = package.Meta.PublicKeyToken

                            }));
                        }
                    }

                    // Verify signature
                    RSACryptoServiceProvider rsa = cert[0].PublicKey.Key as RSACryptoServiceProvider;

                    var retVal = rsa.VerifyData(verifyBytes, CryptoConfig.MapNameToOID("SHA1"), package.Meta.Signature);

                    // Verify timestamp
                    var timestamp = package.Unpack().Info.TimeStamp;
                    if (timestamp > DateTime.Now)
                    {
                        this.m_tracer.TraceError($"Package {package.Meta.Id} was published in the future! Something's fishy, refusing to load");
                        throw new SecurityException(this.m_localizationService.FormatString("error.server.core.packageFishy", new
                        {
                            param = package.Meta.Id
                        }));
                    }
                    else if (cert[0].NotAfter < timestamp || cert[0].NotBefore > timestamp)
                    {
                        this.m_tracer.TraceError($"Cannot find public key of publisher information for {package.Meta.PublicKeyToken} or the local certificate is invalid");
                        throw new SecurityException(this.m_localizationService.FormatString("error.server.core.publicKey", new
                        {
                            param = package.Meta.PublicKeyToken

                        }));
                    }

                    if (retVal == true)
                    {
                        this.m_tracer.TraceEvent(EventLevel.Informational, "SUCCESSFULLY VALIDATED: {0} v.{1}\r\n" +
                            "\tKEY TOKEN: {2}\r\n" +
                            "\tSIGNED BY: {3}\r\n" +
                            "\tVALIDITY: {4:yyyy-MMM-dd} - {5:yyyy-MMM-dd}\r\n" +
                            "\tISSUER: {6}",
                            package.Meta.Id, package.Meta.Version, cert[0].Thumbprint, cert[0].Subject, cert[0].NotBefore, cert[0].NotAfter, cert[0].Issuer);
                    }
                    else
                    {
                        this.m_tracer.TraceEvent(EventLevel.Critical, ">> SECURITY ALERT : {0} v.{1} <<\r\n" +
                            "\tPACKAGE HAS BEEN TAMPERED WITH\r\n" +
                            "\tKEY TOKEN (CLAIMED): {2}\r\n" +
                            "\tSIGNED BY  (CLAIMED): {3}\r\n" +
                            "\tVALIDITY: {4:yyyy-MMM-dd} - {5:yyyy-MMM-dd}\r\n" +
                            "\tISSUER: {6}\r\n\tSERVICE WILL HALT",
                            package.Meta.Id, package.Meta.Version, cert[0].Thumbprint, cert[0].Subject, cert[0].NotBefore, cert[0].NotAfter, cert[0].Issuer);
                    }
                    return retVal;
                }
                finally
                {
                    x509Store.Close();
                }
            }
            else if (this.m_configuration.AllowUnsignedApplets)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "Package {0} v.{1} (publisher: {2}) is not signed. To prevent unsigned applets from being installed disable the configuration option", package.Meta.Id, package.Meta.Version, package.Meta.Author);
                return true;
            }
            else
            {
                this.m_tracer.TraceEvent(EventLevel.Critical, "Package {0} v.{1} (publisher: {2}) is not signed and cannot be installed", package.Meta.Id, package.Meta.Version, package.Meta.Author);
                return false;
            }
        }

        /// <summary>
        /// Starts the daemon service
        /// </summary>
        public bool Start()
        {
            this.m_tracer.TraceInfo("Starting applet manager service...");

            this.Starting?.Invoke(this, EventArgs.Empty);

            try
            {
                // Load packages from applets/ filesystem directory
                var appletDir = this.m_configuration.AppletDirectory;
                if (!Path.IsPathRooted(appletDir))
                    appletDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), this.m_configuration.AppletDirectory);

                if (!Directory.Exists(appletDir))
                    this.m_tracer.TraceWarning("Applet directory {0} doesn't exist, no applets will be loaded", appletDir);
                else
                {
                    this.m_tracer.TraceEvent(EventLevel.Verbose, "Scanning {0} for applets...", appletDir);
                    foreach (var f in Directory.GetFiles(appletDir).OrderBy(o => o.EndsWith(".sln.pak") ? 0 : 1))
                    {
                        // Try to open the file
                        this.m_tracer.TraceInfo("Loading {0}...", f);
                        using (var fs = File.OpenRead(f))
                        {
                            var pkg = AppletPackage.Load(fs);

                            if (pkg is AppletSolution) // We have loaded a solution
                            {
                                if (this.m_solutions.Any(o => o.Meta.Id == pkg.Meta.Id))
                                {
                                    this.m_tracer.TraceEvent(EventLevel.Critical, "Duplicate solution {0} is not permitted", pkg.Meta.Id);
                                    throw new DuplicateKeyException(this.m_localizationService.FormatString("error.server.core.duplicateSolution", new
                                    {
                                        param = pkg.Meta.Id
                                    }));
                                }
                                else if (!this.Install(pkg as AppletSolution, true) && ApplicationServiceContext.Current.HostType != SanteDBHostType.Configuration)
                                {
                                   this.m_tracer.TraceError($"Could not install applet solution {pkg.Meta.Id}");
                                    throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.appletSolution", new
                                    {
                                        param = pkg.Meta.Id
                                    }));
                                }
                            }
                            else if (this.m_fileDictionary.ContainsKey(pkg.Meta.Id))
                            {
                                this.m_tracer.TraceEvent(EventLevel.Warning, "Skipping duplicate package {0}", pkg.Meta.Id);
                                continue;
                            }
                            else if (this.Install(pkg, true))
                            {
                                //this.m_appletCollection.Add(pkg.Unpack());
                                //this.m_fileDictionary.Add(pkg.Meta.Id, f);
                            }
                            else
                            {
                                this.m_tracer.TraceEvent(EventLevel.Critical, "Cannot proceed while untrusted applets are present");
                                throw new SecurityException(this.m_localizationService.GetString("error.server.core.untrustedApplets"));
                            }
                        }
                    }
                }
            }
            catch (SecurityException e)
            {
                this.m_tracer.TraceEvent(EventLevel.Error, "Error loading applets: {0}", e);
                throw new InvalidOperationException(this.m_localizationService.GetString("error.server.core.untrustedApplets.details"),e);
            }
            catch (Exception ex)
            {
                this.m_tracer.TraceEvent(EventLevel.Error, "Error loading applets: {0}", ex);
                throw;
            }

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            this.m_solutions.Clear();
            this.m_appletCollection.Clear();
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Get applet
        /// </summary>
        public AppletManifest GetApplet(string appletId)
        {
            return this.GetApplet(String.Empty, appletId);
        }

        /// <summary>
        /// Gets the specified applet manifest
        /// </summary>
        public AppletManifest GetApplet(string solutionId, string appletId)
        {
            return this.m_appletCollection[solutionId].FirstOrDefault(o => o.Info.Id == appletId);
        }

        /// <summary>
        /// Gets the specified applet manifest
        /// </summary>
        public ReadonlyAppletCollection GetApplets(string solutionId)
        {
            return this.m_readonlyAppletCollection[solutionId];
        }

        /// <summary>
        /// Load an applet
        /// </summary>
        public bool LoadApplet(AppletManifest applet)
        {
            this.m_tracer.TraceError("Loading applet manifests directly is disabled for security reasons");
            throw new SecurityException(this.m_localizationService.GetString("error.server.core.loadingDisabled"));
        }

        /// <summary>
        /// Install the specified applet solution
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="isUpgrade"></param>
        /// <returns></returns>
        public bool Install(AppletSolution solution, bool isUpgrade = false)
        {
            this.m_tracer.TraceInfo("Installing solution {0}", solution.Meta);

            // TODO: Verify package hash / signature
            if (!this.VerifyPackage(solution))
            {
                this.m_tracer.TraceError("Applet failed validation");
                throw new SecurityException(this.m_localizationService.GetString("error.server.core.applet.failedValidation"));
            }

            this.m_appletCollection.Add(solution.Meta.Id, new AppletCollection());
            this.m_readonlyAppletCollection.Add(solution.Meta.Id, this.m_appletCollection[solution.Meta.Id].AsReadonly());
            this.m_readonlyAppletCollection[solution.Meta.Id].CollectionChanged += (o, e) => this.Changed?.Invoke(o, e);

            // Save the applet
            var appletDir = this.m_configuration.AppletDirectory;
            if (!Path.IsPathRooted(appletDir))
                appletDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), this.m_configuration.AppletDirectory);

            if (!Directory.Exists(appletDir))
                Directory.CreateDirectory(appletDir);

            // Install
            var pakFile = Path.Combine(appletDir, solution.Meta.Id + ".pak");
            if (this.m_solutions.Any(o => o.Meta.Id == solution.Meta.Id) && File.Exists(pakFile) && !isUpgrade) 
            {
                this.m_tracer.TraceError($"Cannot replace {solution.Meta} unless upgrade is specifically specified");
                throw new InvalidOperationException(this.m_localizationService.FormatString("error.server.core.upgrade", new
                {
                    param = solution.Meta
                }));      
            }

            // Unpack items from the solution package and install if needed
            foreach (var itm in solution.Include.Where(o => o.Manifest != null))
            {
                var installedApplet = this.GetApplet(solution.Meta.Id, itm.Meta.Id);
                if (installedApplet == null ||
                    new Version(installedApplet.Info.Version) < new Version(itm.Meta.Version)) // Installed version is there but is older or is not installed, so we install it
                {
                    this.m_tracer.TraceInfo("Installing Solution applet {0} v{1}...", itm.Meta.Id, itm.Meta.Version);
                    this.Install(itm, true, solution);
                }
            }

            // Register the pakfile
            lock (this.m_fileDictionary)
                if (!this.m_fileDictionary.ContainsKey(solution.Meta.Id + ".sln"))
                    this.m_fileDictionary.Add(solution.Meta.Id + ".sln", pakFile);

            this.m_solutions.Add(solution);

            foreach (var apl in this.m_appletCollection[solution.Meta.Id])
            {
                var existing = this.m_appletCollection[String.Empty].FirstOrDefault(o => o.Info.Id == apl.Info.Id);
                if (existing == null || new Version(existing.Info.Version) < new Version(apl.Info.Version))
                {
                    this.m_appletCollection[String.Empty].Remove(apl);
                    this.m_appletCollection[String.Empty].Add(apl);
                    this.m_readonlyAppletCollection[String.Empty] = this.m_appletCollection[String.Empty].AsReadonly();
                }
            }
            return true;
        }
    }
}