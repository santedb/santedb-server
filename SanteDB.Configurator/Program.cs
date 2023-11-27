/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Configuration;
using SanteDB.Core.Attributes;
using SanteDB.Core.Configuration;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    [ExcludeFromCodeCoverage]
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            // Check whether the user is in Windows Admin mode
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

#if !DEBUG
                if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    !principal.IsInRole(WindowsBuiltInRole.Administrator) &&
                    args?.Any(a=>a == "--nonadmin") != true)
                {
                    string cmdLine = Environment.CommandLine.Substring(Environment.CommandLine.IndexOf(".exe") + 4);
                    cmdLine = cmdLine.Contains(' ') ? cmdLine.Substring(cmdLine.IndexOf(" ")) : null;
                    ProcessStartInfo psi = new ProcessStartInfo(Assembly.GetEntryAssembly().Location, cmdLine);
                    psi.Verb = "runas";
                    Trace.TraceInformation("Not administrator!");
                    Process proc = Process.Start(psi);
                    Application.Exit();
                    return;
                }
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.SetData(
               "DataDirectory",
               Path.GetDirectoryName(typeof(Program).Assembly.Location));

            // Current dir
            var cwd = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            // Load assembly
            var splash = new frmSplash();
            splash.Show();

            // Load assemblies
            var fileList = Directory.GetFiles(cwd, "Sante*.dll");
            int i = 0;
            foreach (var file in fileList)
            {
                try
                {
                    splash.NotifyStatus($"Loading {Path.GetFileNameWithoutExtension(file)}...", ((float)(i++) / fileList.Length) * 0.5f);
                    var asm = Assembly.LoadFile(file);
                    // Now load all plugins on the assembly
                    var pluginInfo = asm.GetCustomAttribute<PluginAttribute>();
                    if (pluginInfo != null)
                    {
                        ConfigurationContext.Current.PluginAssemblies.Add(asm);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError("Unable to load {0}: {1}", file, e);
                }
            }

            ConfigurationContext.Current.InitializeFeatures();

            // Load the current configuration
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                splash.NotifyStatus("Loading Configuration....", 0.6f);
                if (!File.Exists(ConfigurationContext.Current.ConfigurationFile))
                {
                    splash.NotifyStatus("Preparing initial configuration...", 1f); // TODO: Launch initial configuration
                    splash.Close();
                    ConfigurationContext.Current.InitialStart();

                    var init = new frmInitialConfig();
                    try
                    {
                        if (init.ShowDialog() == DialogResult.Cancel)
                        {
                            return;
                        }
                    }
                    finally
                    {
                        init.Dispose();
                    }
                }
                else if (!ConfigurationContext.Current.LoadConfiguration(ConfigurationContext.Current.ConfigurationFile))
                {
                    splash.Close();
                    return;
                }
                else
                {
                    splash.NotifyStatus("Loading Configuration...", -1f);
                    splash.Close();
                }

                frmMain frmMain = new frmMain();
                // Check for updates
                foreach (var t in ConfigurationContext.Current.Features
                    .Where(o => o.Flags.HasFlag(FeatureFlags.AlwaysConfigure) && !o.Flags.HasFlag(FeatureFlags.SystemFeature))
                    .SelectMany(o => o.CreateInstallTasks())
                    .Where(o => o.VerifyState(ConfigurationContext.Current.Configuration)))
                {
                    ConfigurationContext.Current.ConfigurationTasks.Add(t);
                }

                ConfigurationContext.Current.Apply(frmMain);
                Application.Run(frmMain);
            }
            catch (TargetInvocationException e)
            {
                MessageBox.Show(e.ToHumanReadableString(), "Error Starting Config Tool");
            }
            catch (Exception e)
            {
                Console.WriteLine("Configuration Tooling Fatal Error: {0}", e);
                MessageBox.Show(e.ToHumanReadableString(), "Error Starting Config Tool");
            }
            finally
            {
                Environment.Exit(0);
            }
        }


        /// <summary>
        /// Assembly resolution
        /// </summary>
        internal static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (args.Name == asm.FullName)
                {
                    return asm;
                }
            }

            /// Try for an non-same number Version
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string fAsmName = args.Name;
                if (fAsmName.Contains(","))
                {
                    fAsmName = fAsmName.Substring(0, fAsmName.IndexOf(","));
                }

                if (fAsmName == asm.GetName().Name)
                {
                    return asm;
                }
            }

            // Try based on the file - note: NUGET for large projects sucks at version resolution since it just
            // overwrites a file (for example System.Text.Json) with the last compiled version instead of the
            // the newest so you get errors with System.Text.Json 7.0.0 not resolving when 7.0.3 is not available
            var asmName = args.Name;
            var asmVersion = new Version(1, 0, 0, 0);
            if (asmName.Contains(","))
            {
                asmName = asmName.Substring(0, args.Name.IndexOf(","));
                asmVersion = new Version(args.Name.Split(',').FirstOrDefault(o => o.StartsWith(" Version"))?.Substring(9));
            }

            var asmFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), asmName) + ".dll";
            if(File.Exists(asmFile))
            {
                try
                {
                    
                    var reflectionOnly = Assembly.ReflectionOnlyLoadFrom(asmFile);
                    var reflectionVersion = reflectionOnly.GetName().Version;
                    if(asmVersion.Major == reflectionVersion.Major && asmVersion.Minor == reflectionVersion.Minor)
                    {
                        return Assembly.LoadFrom(asmFile);
                    }
                }
                catch { }
            }
            return null;
        }
    }
}