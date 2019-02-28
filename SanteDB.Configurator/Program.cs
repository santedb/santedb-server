using SanteDB.Core;
using SanteDB.Core.Attributes;
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configurator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.SetData(
               "DataDirectory",
               Path.GetDirectoryName(typeof(Program).Assembly.Location));
            ApplicationServiceContext.Current = ConfigurationContext.Current;

            // Current dir
            var cwd = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            // Load assembly
            var splash = new frmSplash();
            splash.Show();

            // Load assemblies
            var fileList = Directory.GetFiles(cwd, "*.dll");
            int i = 0;
            foreach (var file in fileList)
            {
                try
                {
                    splash.NotifyStatus($"Loading {Path.GetFileNameWithoutExtension(file)}...", ((float)(i++) / fileList.Length) * 0.5f);
                    var asm = Assembly.LoadFile(file);
                    // Now load all plugins on the assembly
                    var pluginInfo = asm.GetCustomAttribute<PluginAttribute>();
                    if(pluginInfo != null)
                        ConfigurationContext.Current.PluginAssemblies.Add(asm);
                }
                catch(Exception e)
                {
                    Trace.TraceError("Unable to load {0}: {1}", file, e);
                }
            }

            // Load the current configuration
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                splash.NotifyStatus("Loading Configuration....", 0.6f);
                if (!File.Exists(ConfigurationContext.Current.ConfigurationFile))
                {
                    splash.NotifyStatus("Preparing initial configuration...", 1f); // TOOD: Launch initial configuration
                    splash.Close();

                    var init = new frmInitialConfig();
                    if (init.ShowDialog() == DialogResult.Cancel)
                        return;
                }
                else if (!ConfigurationContext.Current.LoadConfiguration(ConfigurationContext.Current.ConfigurationFile))
                {
                    splash.Close();
                    return;
                }
                else
                {
                    splash.NotifyStatus("Loading Configuration...", -1f);
                    ConfigurationContext.Current.Start();
                    splash.Close();
                }

                // Check for updates
                foreach (var t in ConfigurationContext.Current.Features
                    .Where(o => o.Flags.HasFlag(FeatureFlags.AlwaysConfigure))
                    .SelectMany(o => o.CreateInstallTasks())
                    .Where(o => o.VerifyState(ConfigurationContext.Current.Configuration)))
                    ConfigurationContext.Current.ConfigurationTasks.Add(t);
                ConfigurationContext.Current.Apply();

                Application.Run(new frmMain());
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Assembly resolution
        /// </summary>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                if (args.Name == asm.FullName)
                    return asm;

            /// Try for an non-same number Version
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string fAsmName = args.Name;
                if (fAsmName.Contains(","))
                    fAsmName = fAsmName.Substring(0, fAsmName.IndexOf(","));
                if (fAsmName == asm.GetName().Name)
                    return asm;
            }

            return null;
        }
    }
}
