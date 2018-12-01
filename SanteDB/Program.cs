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
 * Date: 2018-6-22
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Data;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Xml.Serialization;
using SanteDB.Core.Model;
using System.Collections;
using SanteDB.Core.Model.Attributes;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;

namespace SanteDB
{
    /// <summary>
    /// Guid for the service
    /// </summary>
    [Guid("21F35B18-E417-4F8E-B9C7-73E98B7C71B8")]
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(String[] args)
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
               Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "Data"));

            // Handle Unahndled exception
            AppDomain.CurrentDomain.UnhandledException += (o, e) => {
                Trace.TraceError("++++++ FATAL APPLICATION ERROR ++++++++\r\n{0}", e.ExceptionObject);
                Environment.Exit(999);
            };

            
            // Parser
            ParameterParser<ConsoleParameters> parser = new ParameterParser<ConsoleParameters>();
           
            bool hasConsole = true;
            
            try
            {
                var parameters = parser.Parse(args);
                EntitySource.Current = new EntitySource(new PersistenceEntitySource());

                // What to do?
                if (parameters.ShowHelp)
                    parser.WriteHelp(Console.Out);
                else if(parameters.GenConfig)
                {

                    SanteDBConfiguration configuration = new SanteDBConfiguration();
                    SanteDBServerConfiguration serverConfiguration = new SanteDBServerConfiguration();
                    Console.WriteLine("Will generate full default configuration...");
                    foreach(var file in Directory.GetFiles(Path.GetDirectoryName(typeof(Program).Assembly.Location), "*.dll"))
                    {
                        try
                        {
                            var asm = Assembly.LoadFile(file);
                            Console.WriteLine("Adding service providers from {0}...", file);
                            serverConfiguration.ServiceProviders.AddRange(asm.ExportedTypes.Where(t => typeof(IServiceImplementation).IsAssignableFrom(t) && !t.IsAbstract && !t.ContainsGenericParameters && t.GetCustomAttribute<ServiceProviderAttribute>() != null).Select(o => new TypeReferenceConfiguration(o)));
                            Console.WriteLine("Adding sections from {0}...", file);
                            configuration.Sections.AddRange(asm.ExportedTypes.Where(t => typeof(IConfigurationSection).IsAssignableFrom(t)).Select(t => CreateFullXmlObject(t)));
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("Skipping {0} due to {1}", file, e.Message);
                        }
                    }

                    configuration.RemoveSection<SanteDBServerConfiguration>();
                    serverConfiguration.ThreadPoolSize = Environment.ProcessorCount;
                    configuration.AddSection(serverConfiguration);

                    using (var fs = File.Create(Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "default.config.xml")))
                        configuration.Save(fs);


                }
                else if(parameters.ConsoleMode)
                {
#if DEBUG
                    Core.Diagnostics.Tracer.AddWriter(new Core.Diagnostics.LogTraceWriter(System.Diagnostics.Tracing.EventLevel.LogAlways, "SanteDB.data"), System.Diagnostics.Tracing.EventLevel.LogAlways);
#else
                    Core.Diagnostics.Tracer.AddWriter(new Core.Diagnostics.LogTraceWriter(System.Diagnostics.Tracing.EventLevel.LogAlways, "SanteDB.data"), System.Diagnostics.Tracing.EventLevel.LogAlways);
#endif

                    Console.WriteLine("SanteDB (SanteDB) {0} ({1})", entryAsm.GetName().Version, entryAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
                    Console.WriteLine("{0}", entryAsm.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright);
                    Console.WriteLine("Complete Copyright information available at http://SanteDB.codeplex.com/wikipage?title=Contributions");
                    ApplicationServiceContext.Current = ApplicationContext.Current;
                    ServiceUtil.Start(typeof(Program).GUID);
                    (ApplicationServiceContext.Current as IServiceManager).AddServiceProvider(typeof(FileConfigurationService));
                    ApplicationServiceContext.HostType = SanteDBHostType.Server;
                    if (!parameters.StartupTest)
                    {
                        Console.WriteLine("Type [stop] to stop service...");
                        String input = null;
                        while (input != "stop")
                        {
                            input = Console.ReadLine();
                        }
                    }
                    Console.WriteLine("Shutting down service...");
                    ServiceUtil.Stop();
                }
                else
                {
                    hasConsole = false;
                    ServiceBase[] servicesToRun = new ServiceBase[] { new SanteDB() };
                    ServiceBase.Run(servicesToRun);
                }
            }
            catch(Exception e)
            {
#if DEBUG
                Trace.TraceError("011 899 981 199 911 9725 3!!! {0}", e.ToString());
                if (hasConsole)
                    Console.WriteLine("011 899 981 199 911 9725 3!!! {0}", e.ToString());
#else
                Trace.TraceError("Error encountered: {0}. Will terminate", e.Message);
#endif
                Environment.Exit(911);

            }

        }

        /// <summary>
        /// Create a full XML object
        /// </summary>
        private static object CreateFullXmlObject(Type t)
        {
            var instance = Activator.CreateInstance(t);
            foreach(var prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                try
                {
                    if (prop.GetCustomAttribute<XmlAttributeAttribute>() != null)
                    {
                        prop.SetValue(instance, GetDefaultValue(prop.PropertyType));
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
                            var defaultValue = GetDefaultValue(prop.PropertyType.GetGenericArguments()[0]);
                            if (defaultValue != null)
                                value.Add(defaultValue);
                            else
                                value.Add(CreateFullXmlObject(xeType ?? prop.PropertyType.GetGenericArguments()[0]));

                        }
                        else
                        {
                            // TODO : Choice
                            var defaultValue = GetDefaultValue(prop.PropertyType);
                            if (defaultValue != null)
                                prop.SetValue(instance, defaultValue);
                            else
                                prop.SetValue(instance, CreateFullXmlObject(xeType ?? prop.PropertyType));
                        }

                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("\tSkipping {0}.{1} > {2}", t.FullName, prop.Name, e.Message);
                }
            }
            return instance;
        }

        /// <summary>
        /// Get default value for XML
        /// </summary>
        private static object GetDefaultValue(Type propertyType)
        {
            if(propertyType == typeof(byte[]))
            {
                Console.Write("Data for byte[] value:");
                var data = Console.ReadLine();
                return SHA256.Create().ComputeHash(Console.InputEncoding.GetBytes(data));
            }
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
                        return Enum.ToObject(propertyType, 0);
                    }
                    return null;
            }
        }
    }
}
