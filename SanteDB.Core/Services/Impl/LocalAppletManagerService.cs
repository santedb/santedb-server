﻿/*
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
 * Date: 2018-6-22
 */
using SanteDB.Core.Applets;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.DataTypes;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents an applet manager service that uses the local file system
    /// </summary>
    [ServiceProvider("Local Applet Repository/Manager")]
    public class LocalAppletManagerService : IAppletManagerService, IAppletSolutionManagerService, IDaemonService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Applet Repository/Manager";

        // Solutions registered
        private List<AppletSolution> m_solutions = new List<AppletSolution>();

        // Applet collection
        private AppletCollection m_appletCollection = new AppletCollection();

        // RO applet collection
        private ReadonlyAppletCollection m_readonlyAppletCollection;

        // Map of package id to file
        private Dictionary<String, String> m_fileDictionary = new Dictionary<string, string>();

        // Config file
        private SecurityConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        // Tracer
        private TraceSource m_tracer = new TraceSource(SanteDBConstants.ServiceTraceSourceName);

        /// <summary>
        /// Indicates whether the service is running 
        /// </summary>
        public bool IsRunning => true;

        /// <summary>
        /// Local applet manager ctor
        /// </summary>
        public LocalAppletManagerService()
        {
            this.m_appletCollection = new AppletCollection();
            this.m_readonlyAppletCollection = this.m_appletCollection.AsReadonly();
        }

        /// <summary>
        /// Gets the loaded applets from the manager
        /// </summary>
        public ReadonlyAppletCollection Applets
        {
            get
            {
                return this.m_readonlyAppletCollection;
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
        /// Get the specified package data
        /// </summary>
        public byte[] GetPackage(String appletId)
        {
            this.m_tracer.TraceInformation("Retrieving package {0}", appletId);

            // Save the applet
            var appletDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "applets");
            if (!Directory.Exists(appletDir))
                Directory.CreateDirectory(appletDir);

            // Install
            String pakFile = null;
            if(this.m_fileDictionary.TryGetValue(appletId, out pakFile) && File.Exists(pakFile))
                return File.ReadAllBytes(pakFile);
            else
                throw new FileNotFoundException($"Applet {appletId} not found");
        }

        /// <summary>
        /// Uninstall the applet package
        /// </summary>
        public bool UnInstall(String packageId)
        {

            this.m_tracer.TraceInformation("Un-installing {0}", packageId);
            // Applet check
            var applet = this.m_appletCollection.FirstOrDefault(o => o.Info.Id == packageId);
            if (applet == null) // Might be solution
            {
                var soln = this.m_solutions.FirstOrDefault(o => o.Meta.Id == packageId);
                if(soln == null)
                    throw new FileNotFoundException($"Applet {packageId} is not installed");
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
                var dependencies = this.m_appletCollection.Where(o => o.Info.Dependencies.Any(d => d.Id == packageId));
                if (dependencies.Any())
                    throw new InvalidOperationException($"Uninstalling {packageId} would break : {String.Join(", ", dependencies.Select(o => o.Info))}");

                // We're good to go!
                this.m_appletCollection.Remove(applet);

                lock (this.m_fileDictionary)
                    if (this.m_fileDictionary.ContainsKey(packageId))
                        File.Delete(this.m_fileDictionary[packageId]);

                AppletCollection.ClearCaches();
            }
            return true;
        }

        /// <summary>
        /// Performs an installation 
        /// </summary>
        public bool Install(AppletPackage package, bool isUpgrade = false)
        {
            this.m_tracer.TraceInformation("Installing {0}", package.Meta);

            // TODO: Verify package hash / signature
            if (!this.VerifyPackage(package))
                throw new SecurityException("Applet failed validation");
            else if (!this.m_appletCollection.VerifyDependencies(package.Meta))
                throw new InvalidOperationException($"Applet {package.Meta} depends on : [{String.Join(", ", package.Meta.Dependencies.Select(o=>o.ToString()))}] which are missing or incompatible");
           
            // Save the applet
            var appletDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "applets");
            if (!Directory.Exists(appletDir))
                Directory.CreateDirectory(appletDir);

            // Install
            var pakFile = Path.Combine(appletDir, package.Meta.Id + ".pak");
            if (this.m_appletCollection.Any(o=>o.Info.Id == package.Meta.Id) && File.Exists(pakFile) && !isUpgrade)
                throw new InvalidOperationException($"Cannot replace {package.Meta} unless upgrade is specifically specified");

	        using (var fs = File.Create(pakFile))
	        {
				package.Save(fs);
			}

	        lock (this.m_fileDictionary)
		        if (!this.m_fileDictionary.ContainsKey(package.Meta.Id))
			        this.m_fileDictionary.Add(package.Meta.Id, pakFile);

            var pkg = package.Unpack();

			// remove the package from the collection if this is an upgrade
	        if (isUpgrade)
	        {
		        this.m_appletCollection.Remove(pkg);
	        }

            this.m_appletCollection.Add(pkg);

            // We want to install the templates & protocols into the DB
            this.m_tracer.TraceInformation("Installing templates...");

            // Install templates
            var idp = ApplicationServiceContext.Current.GetService<ITemplateDefinitionRepositoryService>();
            if(idp != null)
                foreach(var itm in pkg.Templates)
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

            // First check: Hash - Make sure the HASH is ok
            if (Convert.ToBase64String(SHA256.Create().ComputeHash(package.Manifest)) != Convert.ToBase64String(package.Meta.Hash))
                throw new InvalidOperationException($"Package contents of {package.Meta.Id} appear to be corrupt!");

            if (package.Meta.Signature != null)
            {
                this.m_tracer.TraceInformation("Will verify package {0}", package.Meta.Id.ToString());

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
                            var embCert = new X509Certificate2(package.PublicKey);
                            // Build the certificate chain
                            var embChain = new X509Chain();
                            embChain.Build(embCert);

                            // Validate the chain elements
                            bool isTrusted = false;
                            foreach (var itm in embChain.ChainElements)
                                isTrusted |= this.m_configuration.TrustedPublishers.Contains(itm.Certificate.Thumbprint);

                            if (!isTrusted || embChain.ChainStatus.Any(o=>o.Status != X509ChainStatusFlags.RevocationStatusUnknown))
                                throw new SecurityException($"Cannot verify identity of publisher {embCert.Subject}");
                            else
                                cert = new X509Certificate2Collection(embCert);
                        }
                        else
                            throw new SecurityException($"Cannot find public key of publisher information for {package.Meta.PublicKeyToken} or the local certificate is invalid");
                    }

                    if (cert[0].NotAfter < DateTime.Now || cert[0].NotBefore > DateTime.Now)
                            throw new SecurityException($"Cannot find public key of publisher information for {package.Meta.PublicKeyToken} or the local certificate is invalid");

                    
                    RSACryptoServiceProvider rsa = cert[0].PublicKey.Key as RSACryptoServiceProvider;
                    var retVal =  rsa.VerifyData(package.Manifest, CryptoConfig.MapNameToOID("SHA1"), package.Meta.Signature);

                    if(retVal == true)
                    {
                        this.m_tracer.TraceEvent(TraceEventType.Information, 0, "SUCCESSFULLY VALIDATED: {0} v.{1}\r\n" +
                            "\tKEY TOKEN: {2}\r\n" +
                            "\tSIGNED BY: {3}\r\n" +
                            "\tVALIDITY: {4:yyyy-MMM-dd} - {5:yyyy-MMM-dd}\r\n" +
                            "\tISSUER: {6}", 
                            package.Meta.Id, package.Meta.Version, cert[0].Thumbprint, cert[0].Subject, cert[0].NotBefore, cert[0].NotAfter, cert[0].Issuer);
                    }
                    else
                    {
                        this.m_tracer.TraceEvent(TraceEventType.Critical, 0, ">> SECURITY ALERT : {0} v.{1} <<\r\n" +
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
                this.m_tracer.TraceEvent(TraceEventType.Warning, 1099, "Package {0} v.{1} (publisher: {2}) is not signed. To prevent unsigned applets from being installed disable the configuration option", package.Meta.Id, package.Meta.Version, package.Meta.Author);
                return true;
            }
            else
            {
                this.m_tracer.TraceEvent(TraceEventType.Critical, 1097, "Package {0} v.{1} (publisher: {2}) is not signed and cannot be installed", package.Meta.Id, package.Meta.Version, package.Meta.Author);
                return false;
            }
        }

        /// <summary>
        /// Starts the daemon service 
        /// </summary>
        public bool Start()
        {

            this.m_tracer.TraceInformation("Starting applet manager service...");

            this.Starting?.Invoke(this, EventArgs.Empty);

	        try
	        {
		        // Load packages from applets/ filesystem directory
                var appletDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "applets");
                if (!Directory.Exists(appletDir))
                    this.m_tracer.TraceWarning("Applet directory {0} doesn't exist, no applets will be loaded", appletDir);
                else
                {
                    this.m_tracer.TraceEvent(TraceEventType.Verbose, 0, "Scanning {0} for applets...", appletDir);
                    foreach (var f in Directory.GetFiles(appletDir))
                    {
                        // Try to open the file
                        this.m_tracer.TraceInformation("Loading {0}...", f);
                        using (var fs = File.OpenRead(f))
                        {
                            var pkg = AppletPackage.Load(fs);

                            if (pkg is AppletSolution) // We have loaded a solution
                            {
                                if (this.m_solutions.Any(o => o.Meta.Id == pkg.Meta.Id))
                                {
                                    this.m_tracer.TraceEvent(TraceEventType.Critical, 1096, "Duplicate solution {0} is not permitted", pkg.Meta.Id);
                                    throw new DuplicateKeyException(pkg.Meta.Id);
                                }
                                else if(!this.Install(pkg as AppletSolution, true))
                                {
                                    throw new InvalidOperationException($"Could not install applet solution {pkg.Meta.Id}");
                                }
                            }
                            else if (this.m_fileDictionary.ContainsKey(pkg.Meta.Id))
                            {
                                this.m_tracer.TraceEvent(TraceEventType.Critical, 1096, "Duplicate package {0} is not permitted", pkg.Meta.Id);
                                throw new DuplicateKeyException(pkg.Meta.Id);
                            }
                            else if (this.Install(pkg, true))
                            {
                                //this.m_appletCollection.Add(pkg.Unpack());
                                //this.m_fileDictionary.Add(pkg.Meta.Id, f);
                            }
                            else
                            {
                                this.m_tracer.TraceEvent(TraceEventType.Critical, 1098, "Cannot proceed while untrusted applets are present");
                                throw new SecurityException("Cannot proceed while untrusted applets are present");
                            }
                        }

                    }
                }
	        }
	        catch (SecurityException e)
	        {
				this.m_tracer.TraceEvent(TraceEventType.Error, e.HResult, "Error loading applets: {0}", e);
		        throw new InvalidOperationException("Cannot proceed while untrusted applets are present");
			}
            catch (Exception ex)
            {
                this.m_tracer.TraceEvent(TraceEventType.Error, ex.HResult, "Error loading applets: {0}", ex);
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
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Get applet
        /// </summary>
        public AppletManifest GetApplet(string appletId)
        {
            return this.m_appletCollection.FirstOrDefault(o => o.Info.Id == appletId);
        }

        /// <summary>
        /// Load an applet
        /// </summary>
        public bool LoadApplet(AppletManifest applet)
        {
            throw new SecurityException("Loading applet manifests directly is disabled for security reasons");
        }


        /// <summary>
        /// Install the specified applet solution
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="isUpgrade"></param>
        /// <returns></returns>
        public bool Install(AppletSolution solution, bool isUpgrade = false)
        {
            this.m_tracer.TraceInformation("Installing solution {0}", solution.Meta);

            // TODO: Verify package hash / signature
            if (!this.VerifyPackage(solution))
                throw new SecurityException("Applet failed validation");
            else if (!this.m_appletCollection.VerifyDependencies(solution.Meta))
                throw new InvalidOperationException($"Applet {solution.Meta} depends on : [{String.Join(", ", solution.Meta.Dependencies.Select(o => o.ToString()))}] which are missing or incompatible");

            // Save the applet
            var appletDir = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "applets");
            if (!Directory.Exists(appletDir))
                Directory.CreateDirectory(appletDir);

            // Install
            var pakFile = Path.Combine(appletDir, solution.Meta.Id + ".sln.pak");
            if (this.m_solutions.Any(o => o.Meta.Id == solution.Meta.Id) && File.Exists(pakFile) && !isUpgrade)
                throw new InvalidOperationException($"Cannot replace {solution.Meta} unless upgrade is specifically specified");
            
            // Unpack items from the solution package and install if needed
            foreach(var itm in solution.Include.Where(o=>o.Manifest != null))
            {
                var installedApplet = this.GetApplet(itm.Meta.Id);
                if (installedApplet == null ||
                    new Version(installedApplet.Info.Version) < new Version(itm.Meta.Version)) // Installed version is there but is older or is not installed, so we install it
                {
                    this.m_tracer.TraceInfo("Installing Solution applet {0} v{1}...", itm.Meta.Id, itm.Meta.Version);
                    this.Install(itm, true);
                }
                itm.Manifest = null;
            }

            // Register the pakfile
            lock (this.m_fileDictionary)
                if (!this.m_fileDictionary.ContainsKey(solution.Meta.Id + ".sln"))
                    this.m_fileDictionary.Add(solution.Meta.Id + ".sln", pakFile);

            using (var fs = File.Create(pakFile))
            {
                solution.Save(fs);
            }

            this.m_solutions.Add(solution);

            return true;
        }
    }
}
