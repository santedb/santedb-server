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

using MohawkCollege.Util.Console.Parameters;
using Mono.Unix;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Model;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Server;
using SanteDB.Server.Core.Services.Impl;
using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
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

            // Dump some info
            Trace.TraceInformation("SanteDB Startup : v{0}", entryAsm.GetName().Version);
            Trace.TraceInformation("SanteDB Working Directory : {0}", entryAsm.Location);
            Trace.TraceInformation("Operating System: {0} {1}", Environment.OSVersion.Platform, Environment.OSVersion.VersionString);
            Trace.TraceInformation("CLI Version: {0}", Environment.Version);

            AppDomain.CurrentDomain.SetData(
               "DataDirectory",
               Path.GetDirectoryName(typeof(Program).Assembly.Location));

            // Handle Unahndled exception
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
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

                var instanceSuffix = !String.IsNullOrEmpty(parameters.InstanceName) ? $"-{parameters.InstanceName}" : null;

                // What to do?
                if (parameters.ShowHelp)
                    parser.WriteHelp(Console.Out);
                else if (parameters.InstallCerts)
                {
                    Console.WriteLine("Installing security certificates...");
                    SecurityExtensions.InstallCertsForChain();
                }
                else if (parameters.Install)
                {
                    if (!ServiceTools.ServiceInstaller.ServiceIsInstalled($"SanteDB{instanceSuffix}"))
                    {
                        Console.WriteLine("Installing Service...");
                        if (!String.IsNullOrEmpty(instanceSuffix))
                        {
                            var configFile = parameters.ConfigFile;
                            if (String.IsNullOrEmpty(configFile))
                                configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), $"santedb.config.{parameters.InstanceName}.xml");
                            else if (!Path.IsPathRooted(configFile))
                                configFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), configFile);
                            ServiceTools.ServiceInstaller.Install($"SanteDB{instanceSuffix}", $"SanteDB Host Process - {parameters.InstanceName}", $"{Assembly.GetEntryAssembly().Location} --name={parameters.InstanceName} --config={configFile}", null, null, ServiceTools.ServiceBootFlag.AutoStart);
                        }
                        else
                            ServiceTools.ServiceInstaller.Install($"SanteDB", "SanteDB Host Process", $"{Assembly.GetEntryAssembly().Location}", null, null, ServiceTools.ServiceBootFlag.AutoStart);
                    }
                }
                else if (parameters.UnInstall)
                {
                    if (ServiceTools.ServiceInstaller.ServiceIsInstalled($"SanteDB{instanceSuffix}"))
                    {
                        Console.WriteLine("Un-Installing Service...");
                        ServiceTools.ServiceInstaller.StopService($"SanteDB{instanceSuffix}");
                        ServiceTools.ServiceInstaller.Uninstall($"SanteDB{instanceSuffix}");
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
                            serverConfiguration.ServiceProviders.AddRange(asm.ExportedTypes.Where(t => typeof(IServiceImplementation).IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters && t.GetCustomAttribute<ServiceProviderAttribute>() != null).Select(o => new TypeReferenceConfiguration(o)));
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
                        configuration.Save(fs);
                }
                else if (parameters.ConsoleMode)
                {
                    Console.WriteLine("SanteDB (SanteDB) {0} ({1})", entryAsm.GetName().Version, entryAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
                    Console.WriteLine("{0}", entryAsm.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
                    Console.WriteLine("Complete Copyright information available at http://SanteDB.codeplex.com/wikipage?title=Contributions");
                    ServiceUtil.Start(typeof(Program).GUID, new FileConfigurationService(parameters.ConfigFile));
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

                            try // remove the lock file
                            {
                                File.Delete("/tmp/SanteDB.exe.lock");
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    hasConsole = false;
                    ServiceBase[] servicesToRun = new ServiceBase[] { new SanteDBService() };
                    ServiceBase.Run(servicesToRun);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.TraceError("011 899 981 199 911 9725 3!!! {0}", e.ToString());
               

                EventLog.WriteEntry("SanteDB Host Process", $"011 899 981 199 911 9725 3!!! {e}", EventLogEntryType.Error, 911);

#else
                Trace.TraceError("Error encountered: {0}. Will terminate", e);
#endif
                if (hasConsole)
                    Console.WriteLine("011 899 981 199 911 9725 3!!! {0}", e.ToString());
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
                                value.Add(defaultValue);
                            else
                                value.Add(CreateFullXmlObject(xeType ?? prop.PropertyType.GetGenericArguments()[0]));
                        }
                        else
                        {
                            // TODO : Choice
                            var defaultValue = GetDefaultValue(prop.PropertyType, prop.Name);
                            if (defaultValue != null)
                                prop.SetValue(instance, defaultValue);
                            else
                                prop.SetValue(instance, CreateFullXmlObject(xeType ?? prop.PropertyType));
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
                return DateTime.Now.ToString("o");
            else
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