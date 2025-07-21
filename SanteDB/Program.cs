/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using MohawkCollege.Util.Console.Parameters;
using Mono.Unix;
using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite.Configuration;
using SanteDB.OrmLite.Providers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace SanteDB
{
    /// <summary>
    /// Guid for the service
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Guid("21F35B18-E417-4F8E-B9C7-73E98B7C71B8")]
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(String[] args)
        {
            // Trace copyright information
            Assembly entryAsm = Assembly.GetEntryAssembly();

            if (null == entryAsm)
            {
                entryAsm = typeof(Program).Assembly;
            }

            var workdirectory = Path.GetDirectoryName(entryAsm.Location);
            var datadirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);

            if (null == datadirectory || (datadirectory.Equals(workdirectory, StringComparison.OrdinalIgnoreCase) == false))
            {
                Trace.TraceWarning("Warning entry assembly location is not the same as detected data directory.");
            }

            // Dump some info
            Trace.TraceInformation("SanteDB Startup : v{0}", entryAsm.GetName().Version);
            Trace.TraceInformation("SanteDB Working Directory : {0}", entryAsm.Location);
            Trace.TraceInformation("Operating System: {0} {1}", Environment.OSVersion.Platform, Environment.OSVersion.VersionString);
            Trace.TraceInformation("CLI Version: {0}", Environment.Version);
            AppDomain.CurrentDomain.SetData(
               "DataDirectory",
               datadirectory
            );

            // Handle Unahndled exception
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                try
                {
                    ApplicationServiceContext.Current.Stop();
                }
                catch(Exception ex)
                {
                    Trace.TraceError("Could not stop application service context");
                }
                Trace.TraceError("++++++ FATAL APPLICATION ERROR ++++++++\r\n{0}", e.ExceptionObject);
                EventLog.WriteEntry("SanteDB Host Process", $"++++++ FATAL APPLICATION ERROR ++++++++\r\n{e.ExceptionObject}", EventLogEntryType.Error, 999);
                Environment.Exit(999);
            };

            // Parser
            ParameterParser<ConsoleParameters> parser = new ParameterParser<ConsoleParameters>();

            bool hasConsole = true;

            try
            {
                var parameters = parser.Parse(args);

                // Are there any third party libraries to load?
                LoadExtensions(parameters);

                var instanceSuffix = !String.IsNullOrEmpty(parameters.InstanceName) ? $"-{parameters.InstanceName}" : null;
                var serviceName = $"SanteDB{instanceSuffix}";
                // What to do?
                if (parameters.ShowHelp)
                {
                    parser.WriteHelp(Console.Out);
                }
                else if (parameters.ReEncrypt)
                {
                    if (ServiceTools.ServiceInstaller.ServiceIsInstalled(serviceName) &&
                        ServiceTools.ServiceInstaller.GetServiceStatus(serviceName) != ServiceTools.ServiceState.Stop)
                    {
                        Console.WriteLine("Stopping {0}...", serviceName);
                        ServiceTools.ServiceInstaller.StopService(serviceName);
                    }
                    ReEncrypt(parameters.ConfigFile);
                }
                else if (parameters.ConfigTest)
                {
                    TestConfiguration(parameters.ConfigFile);
                }
                else if (parameters.InstallCerts)
                {
                    Console.WriteLine("Installing security certificates...");
                    SecurityExtensions.InstallCertsForChain();
                }
                else if (parameters.Install)
                {
                    if (!ServiceTools.ServiceInstaller.ServiceIsInstalled(serviceName))
                    {
                        Console.WriteLine("Installing Service...");
                        var displayname = "SanteDB Host Process"; //Default display name when no name specified.

                        var configFile = parameters.ConfigFile;

                        if (String.IsNullOrEmpty(configFile))
                        {
                            configFile = Path.Combine(Path.GetDirectoryName(entryAsm.Location), $"santedb.config.{parameters.InstanceName}.xml");
                        }
                        else if (!Path.IsPathRooted(configFile))
                        {
                            configFile = Path.Combine(Path.GetDirectoryName(entryAsm.Location), configFile);
                        }

                        if (!String.IsNullOrEmpty(instanceSuffix))
                        {
                            displayname = $"SanteDB Host Process - {parameters.InstanceName}";
                        }

                        StringBuilder servicecommandline = new StringBuilder(64);

                        servicecommandline.Append('"');
                        servicecommandline.Append(entryAsm.Location);
                        servicecommandline.Append('"');

                        if (!string.IsNullOrWhiteSpace(parameters.InstanceName))
                            servicecommandline.AppendFormat(" --name={0}", parameters.InstanceName);

                        servicecommandline.AppendFormat(" --config={0}", configFile);

                        if (null != parameters.LoadExtensions && parameters.LoadExtensions.Count > 0)
                        {
                            //TODO: Handle wildcards and fix loads for service for security against malicious plugins.
                            foreach (var loadparm in parameters.LoadExtensions)
                            {
                                servicecommandline.AppendFormat(" --load=\"{0}\"", loadparm);
                            }
                        }

                        ServiceTools.ServiceInstaller.Install(serviceName, displayname, servicecommandline.ToString(), null, null, ServiceTools.ServiceBootFlag.AutoStart);
                    }
                }
                else if (parameters.UnInstall)
                {
                    if (ServiceTools.ServiceInstaller.ServiceIsInstalled(serviceName))
                    {
                        Console.WriteLine("Un-Installing Service...");
                        ServiceTools.ServiceInstaller.StopService(serviceName);
                        ServiceTools.ServiceInstaller.Uninstall(serviceName);
                    }
                }
                else if (parameters.GenConfig)
                {
                    SanteDBConfiguration configuration = new SanteDBConfiguration();
                    ApplicationServiceContextConfigurationSection serverConfiguration = new ApplicationServiceContextConfigurationSection();
                    Console.WriteLine("Will generate full default configuration...");
                    foreach (var file in Directory.GetFiles(Path.GetDirectoryName(typeof(Program).Assembly.Location), "*.dll"))
                    {
                        try
                        {
                            var asm = Assembly.LoadFile(file);
                            Console.WriteLine("Adding service providers from {0}...", file);
                            serverConfiguration.ServiceProviders.AddRange(asm.ExportedTypes.Where(t => typeof(IServiceImplementation).IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters && t.HasCustomAttribute<ServiceProviderAttribute>()).Select(o => new TypeReferenceConfiguration(o)));
                            Console.WriteLine("Adding sections from {0}...", file);
                            configuration.Sections.AddRange(asm.ExportedTypes.Where(t => typeof(IConfigurationSection).IsAssignableFrom(t)).Select(t => CreateFullXmlObject(t)));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Skipping {0} due to {1}", file, e.Message);
                        }
                    }

                    configuration.RemoveSection<ApplicationServiceContextConfigurationSection>();
                    serverConfiguration.ThreadPoolSize = Environment.ProcessorCount * 16;
                    configuration.AddSection(serverConfiguration);

                    using (var fs = File.Create(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "default.config.xml")))
                    {
                        configuration.Save(fs);
                    }
                }
                else if (parameters.ConsoleMode)
                {
                    Console.WriteLine("SanteDB (SanteDB) {0} ({1})", entryAsm.GetName().Version, entryAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
                    Console.WriteLine("{0}", entryAsm.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
                    Console.WriteLine("Complete Copyright information available at https://github.com/santedb/santedb/blob/master/NOTICE.md");

                    // Detect platform
                    if (System.Environment.OSVersion.Platform != PlatformID.Win32NT)
                    {
                        Console.WriteLine("Not running on WindowsNT, some features may not function correctly");
                    }
                    else
                    {
                        try
                        {
                            if (!EventLog.SourceExists("SanteDB Host Process"))
                            {
                                EventLog.CreateEventSource("SanteDB Host Process", "santedb");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("WARN: Error creating EventLog source. Not running as admin? {0}", e);
                        }
                    }

                    ServiceUtil.Start(typeof(Program).GUID, new ServerApplicationContext(parameters.ConfigFile));
                    if (!parameters.StartupTest)
                    {
                        // Did the service start properly?
                        if (!ApplicationServiceContext.Current.IsRunning)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Application context did not start properly and is in maintenance mode...");
                            Console.ResetColor();
                        }

                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        {
                            ManualResetEvent quitEvent = new ManualResetEvent(false);

                            Console.CancelKeyPress += (o, e) =>
                            {
                                Console.WriteLine("Service shutting down...");
                                ServiceUtil.Stop();
                                quitEvent.Set();
                            };

                            Console.WriteLine("Service started (CTRL+C to stop)...");
                            quitEvent.WaitOne();
                        }
                        else
                        {
                            // Now wait until the service is exiting va SIGTERM or SIGSTOP
                            UnixSignal[] signals = new UnixSignal[]
                            {
                                new UnixSignal(Mono.Unix.Native.Signum.SIGINT),
                                new UnixSignal(Mono.Unix.Native.Signum.SIGTERM),
                                new UnixSignal(Mono.Unix.Native.Signum.SIGQUIT),
                                new UnixSignal(Mono.Unix.Native.Signum.SIGHUP)
                            };
                            int signal = UnixSignal.WaitAny(signals);
                            // Gracefully shutdown
                            ServiceUtil.Stop();

                            //try // remove the lock file
                            //{
                            //    File.Delete("/tmp/SanteDB.exe.lock");
                            //}
                            //catch { }
                        }
                    }

                }
                else
                {
                    hasConsole = false;
                    ServiceBase[] servicesToRun = new ServiceBase[] { new SanteDBService(args != null && args.Length > 0 ? parameters : null) };
                    ServiceBase.Run(servicesToRun);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.TraceError("011 899 981 199 911 9725 3!!! {0}", e.ToString());

                try
                {
                    if (EventLog.Exists("SanteDB Host Process"))
                    {
                        EventLog.WriteEntry("SanteDB Host Process", $"011 899 981 199 911 9725 3!!! {e}", EventLogEntryType.Error, 911);
                    }
                }
                catch { }
#else
                Trace.TraceError("Error encountered: {0}. Will terminate", e);
#endif
                if (hasConsole)
                {
                    Console.WriteLine("011 899 981 199 911 9725 3!!! {0}", e.ToString());
                }

                try
                {
                    EventLog.WriteEntry("SanteDB Host Process", $"011 899 981 199 911 9725 3!!! {e}", EventLogEntryType.Error, 911);
                }
                catch (Exception e1)
                {
                    Trace.TraceWarning("Could not emit the error to the EventLog - {0}", e1);
                }
                Environment.Exit(911);
            }
        }

        /// <summary>
        /// Loads extensions specified in the command line.
        /// </summary>
        /// <param name="parameters">The parsed command line parameters with the extensions.</param>
        private static bool LoadExtensions(ConsoleParameters parameters)
        {
            if (parameters.LoadExtensions?.Count > 0)
            {
                foreach (var ext in parameters.LoadExtensions)
                {
                    var itm = ext;
                    if (!Path.IsPathRooted(itm))
                    {
                        itm = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), itm);
                    }

                    if (File.Exists(itm))
                    {
                        Console.WriteLine("Loading {0}...", itm); // TODO: Use System.Diagnostics.Tracer
                        Assembly.LoadFile(itm);
                    }
                    else if (itm.Contains("*"))
                    {
                        var directoryName = Path.GetDirectoryName(itm);
                        if (!Directory.Exists(directoryName))
                        {
                            directoryName = Path.GetDirectoryName(directoryName);
                        }
                        foreach (var fil in Directory.GetFiles(directoryName, Path.GetFileName(itm)))
                        {
                            Console.WriteLine("Loading {0}...", fil); // TODO: Use System.Diagnostics.Tracer
                            Assembly.LoadFile(fil);
                        }
                    }
                    else
                    {
                        Console.WriteLine("{0} does not exist", itm);
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Rotate the keys 
        /// </summary>
        private static void ReEncrypt(string configFile)
        {
            SanteDBConfiguration configuration = null;
            using (AuthenticationContext.EnterSystemContext())
            {
                try
                {
                    using (var fs = File.OpenRead(configFile))
                    {
                        configuration = SanteDBConfiguration.Load(fs);
                    }

                    // Rotate the keys - first we want to get the key we're rotating to
                    Console.WriteLine("This command will encrypt your configuration file and your database using ALE (if configured). If the configuration and database are already encrypted, the keys will be rotated.");
                    Console.Write("New Key Thumbprint (enter to disable):");
                    var newKeyThumb = Console.ReadLine();

                    // Attempt to get the certificate
                    if (!String.IsNullOrEmpty(newKeyThumb))
                    {
                        bool isCurrentUser = X509CertificateUtils.GetPlatformServiceOrDefault().TryGetCertificate(X509FindType.FindByThumbprint, newKeyThumb, StoreName.My, out var certificate),
                            isLocalMachine = X509CertificateUtils.GetPlatformServiceOrDefault().TryGetCertificate(X509FindType.FindByThumbprint, newKeyThumb, StoreName.My, StoreLocation.LocalMachine, out certificate);
                        if (!isCurrentUser && !isLocalMachine)
                        {
                            throw new InvalidOperationException("Cannot find certificate in CurrentUser\\My or LocalMachine\\My");
                        }

                        // Replace the key
                        configuration.ProtectedSectionKey = new Core.Security.Configuration.X509ConfigurationElement(isLocalMachine ? StoreLocation.LocalMachine : StoreLocation.CurrentUser, StoreName.My, X509FindType.FindByThumbprint, newKeyThumb);
                    }
                    else
                    {
                        configuration.ProtectedSectionKey = null;
                    }

                    // If ALE is enabled then we want to recrypt
                    var processedConnections = new List<String>();
                    foreach (var ormConfiguration in configuration.Sections.OfType<OrmConfigurationBase>())
                    {
                        if (ormConfiguration?.AleConfiguration?.AleEnabled != true)
                            continue;


                        processedConnections.Add(ormConfiguration.ReadWriteConnectionString);

                        // Decrypt and recrypt the ALE
                        try
                        {
                            var ormSection = configuration.GetSection<OrmConfigurationSection>();
                            var connectionString = configuration.GetSection<DataConfigurationSection>()?.ConnectionString.Find(o => o.Name.Equals(ormConfiguration.ReadWriteConnectionString, StringComparison.OrdinalIgnoreCase));
                            var providerType = ormSection?.Providers.Find(o => o.Invariant == connectionString.Provider).Type;
                            var provider = Activator.CreateInstance(providerType) as IEncryptedDbProvider;

                            if (provider == null)
                            {
                                continue;
                            }

                            provider.ConnectionString = connectionString.ToString();

                            // Decrypt - 
                            Console.WriteLine("Rotating keys for {0} (this may take several hours)...", ormConfiguration.GetType().Name);
                            provider.SetEncryptionSettings(ormConfiguration.AleConfiguration);

                            provider.GetEncryptionProvider();

                            ormConfiguration.AleConfiguration = new OrmAleConfiguration()
                            {
                                AleEnabled = configuration.ProtectedSectionKey != null,
                                Certificate = configuration.ProtectedSectionKey,
                                EnableFields = ormConfiguration.AleConfiguration.EnableFields,
                                SaltSeed = ormConfiguration.AleConfiguration.SaltSeed
                            };
                            provider.MigrateEncryption(ormConfiguration.AleConfiguration);
                        }
                        catch (Exception e)
                        {
                            throw new DataException($"Cannot migrate ALE on {ormConfiguration.ReadWriteConnectionString}", e);
                        }
                    }

                    using (var fs = File.Create(configFile))
                    {
                        configuration.Save(fs);
                    }
                }
                catch (Exception e)
                {
                    throw new DataException($"Cannot migrate ALE", e);
                }
            }
        }

        private static void TestConfiguration(string configFile)
        {
            IEnumerable<String> configFilesToTest = null;
            if (String.IsNullOrEmpty(configFile))
            {
                configFilesToTest = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "config"), "*.xml", SearchOption.AllDirectories);
            }
            else
            {
                configFilesToTest = new String[] { configFile };
            }
            Console.WriteLine("Testing configuration files");
            foreach (var file in configFilesToTest)
            {
                try
                {
                    Console.WriteLine("Testing {0}...", file);
                    using (var stream = File.OpenRead(file))
                    {
                        foreach (DetectedIssue itm in SanteDBConfiguration.Validate(stream))
                        {
                            Console.WriteLine("\t{0} - {1}", itm.Priority, itm.Text);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\tFAIL: {0}", e);
                }
            }
        }

        /// <summary>
        /// Create a full XML object
        /// </summary>
        private static object CreateFullXmlObject(Type t)
        {
            var instance = Activator.CreateInstance(t);
            foreach (var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                try
                {
                    if (prop.GetCustomAttribute<XmlAttributeAttribute>() != null)
                    {
                        prop.SetValue(instance, GetDefaultValue(prop.PropertyType, prop.Name));
                    }
                    else if (prop.GetCustomAttributes<XmlElementAttribute>().Count() > 0 ||
                        prop.GetCustomAttribute<XmlArrayAttribute>() != null)
                    {
                        var xeType = prop.GetCustomAttributes<XmlElementAttribute>().FirstOrDefault()?.Type;
                        // List object
                        if (typeof(IList).IsAssignableFrom(prop.PropertyType))
                        {
                            var value = Activator.CreateInstance(prop.PropertyType) as IList;
                            prop.SetValue(instance, value);
                            var defaultValue = GetDefaultValue(prop.PropertyType.GetGenericArguments()[0], prop.Name);
                            if (defaultValue != null)
                            {
                                value.Add(defaultValue);
                            }
                            else
                            {
                                value.Add(CreateFullXmlObject(xeType ?? prop.PropertyType.GetGenericArguments()[0]));
                            }
                        }
                        else
                        {
                            var defaultValue = GetDefaultValue(prop.PropertyType, prop.Name);
                            if (defaultValue != null)
                            {
                                prop.SetValue(instance, defaultValue);
                            }
                            else
                            {
                                prop.SetValue(instance, CreateFullXmlObject(xeType ?? prop.PropertyType));
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("\tSkipping {0}.{1} > {2}", t.FullName, prop.Name, e.Message);
                }
            }
            return instance;
        }

        /// <summary>
        /// Get default value for XML
        /// </summary>
        private static object GetDefaultValue(Type propertyType, String fieldName)
        {
            if (propertyType == typeof(byte[]))
            {
                Console.Write("Data for byte[] for {0} value:", fieldName);
                var data = Console.ReadLine();
                return SHA256.Create().ComputeHash(Console.InputEncoding.GetBytes(data));
            }
            else if (fieldName.StartsWith("Xml"))
            {
                return DateTime.Now.ToString("o");
            }
            else
            {
                switch (propertyType.StripNullable().Name.ToLower())
                {
                    case "bool":
                    case "boolean":
                        return false;

                    case "string":
                        return "value";

                    case "guid":
                        return Guid.Empty;

                    case "int":
                    case "int32":
                    case "int64":
                    case "long":
                    case "short":
                    case "int16":
                        return 0;

                    case "double":
                    case "float":
                        return 0.0f;

                    case "datetime":
                        return DateTime.MinValue;

                    case "timespan":
                        return TimeSpan.MinValue;

                    case "storename":
                        return StoreName.My;

                    case "storelocation":
                        return StoreLocation.CurrentUser;

                    case "x509findtype":
                        return X509FindType.FindByThumbprint;

                    default:
                        if (propertyType.IsEnum)
                        {
                            var field1 = propertyType.GetFields().Skip(1).FirstOrDefault();
                            return Enum.ToObject(propertyType, field1.GetValue(null));
                        }
                        return null;
                }
            }
        }
    }
}