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
 * Date: 2018-11-26
 */
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB
{
    /// <summary>
    /// Utility for starting / stopping hosted services
    /// </summary>
    public static class ServiceUtil
    {


        /// <summary>
        /// Helper function to start the services
        /// </summary>
        public static int Start(Guid activityId)
        {
            Trace.CorrelationManager.ActivityId = activityId;
            Trace.TraceInformation("Starting host context on Console Presentation System at {0}", DateTime.Now);

            // Detect platform
            if (System.Environment.OSVersion.Platform != PlatformID.Win32NT)
                Trace.TraceWarning("Not running on WindowsNT, some features may not function correctly");

            // Do this because loading stuff is tricky ;)
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            try
            {
                // Initialize 
                ApplicationServiceContext.Current = ApplicationContext.Current;
                (ApplicationServiceContext.Current as IServiceManager).AddServiceProvider(typeof(FileConfigurationService));
                ApplicationServiceContext.HostType = SanteDBHostType.Server;

                Trace.TraceInformation("Getting default message handler service.");
                if (ApplicationContext.Current.Start())
                {
                    Trace.TraceInformation("Service Started Successfully");
                    return 0;
                }
                else
                {
                    Trace.TraceError("No message handler service started. Terminating program");
                    Stop();
                    return 1911;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Fatal exception occurred: {0}", e.ToString());
                Stop();
                return 1064;
            }
            finally
            {
            }
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public static void Stop()
        {
            ApplicationContext.Current.Stop();
        }

        /// <summary>
        /// Assembly resolution
        /// </summary>
        internal static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
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
