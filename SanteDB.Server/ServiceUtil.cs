﻿/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using SanteDB.Core;
using SanteDB.Core.Data;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Services;
using SanteDB.Core.Services.Impl;
using SanteDB.Server.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server
{
    /// <summary>
    /// Utility for starting / stopping hosted services
    /// </summary>
    public static class ServiceUtil
    {


        /// <summary>
        /// Helper function to start the services
        /// </summary>
        public static int Start(Guid activityId, IConfigurationManager configManager)
        {
            Trace.CorrelationManager.ActivityId = activityId;
            Trace.TraceInformation("Starting host context on Console Presentation System at {0}", DateTime.Now);

            // Detect platform
            if (System.Environment.OSVersion.Platform != PlatformID.Win32NT)
                Trace.TraceWarning("Not running on WindowsNT, some features may not function correctly");
            else try
                {
                    if (!EventLog.SourceExists("SanteDB Host Process"))
                        EventLog.CreateEventSource("SanteDB Host Process", "santedb");
                }
                catch(Exception e)
                {
                    Trace.TraceWarning("Error creating EventLog source. Not running as admin? {0}", e);
                }

            // Do this because loading stuff is tricky ;)
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

            try
            {
                // Initialize 
                ApplicationServiceContext.Current = ServerApplicationContext.Current;
                ApplicationServiceContext.Current.GetService<IServiceManager>().AddServiceProvider(configManager);
                EntitySource.Current = new EntitySource(new PersistenceEntitySource());

                Trace.TraceInformation("Getting default message handler service.");
                if (ServerApplicationContext.Current.Start())
                {
                    Trace.TraceInformation("Service Started Successfully");
                    try
                    {
                        EventLog.WriteEntry("SanteDB Host Process", $"SanteDB is ready to accept connections", EventLogEntryType.Information, 777);
                    }
                    catch { }
                    return 0;
                }
                else
                {
                    Trace.TraceError("No message handler service started. Terminating program");
                    try // This is an ignorable error since we're just emitting to EVT Log
                    {
                        EventLog.WriteEntry("SanteDB Host Process", $"Please configure a  message handler to run this service", EventLogEntryType.Error, 1911);
                    }
                    catch { }
                    Stop();
                    return 1911;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Fatal exception occurred: {0}", e.ToString());

                try
                {
                    EventLog.WriteEntry("SanteDB Host Process", $"Exception occurred starting up: {e}", EventLogEntryType.Error, 1064);
                }
                catch { }

                Stop();
                throw;
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
            try
            {
                EventLog.WriteEntry("SanteDB Host Process", $"The SanteDB service is shutting down services", EventLogEntryType.Information, 666);
            }
            catch { }

            ServerApplicationContext.Current.Stop();
            Tracer.DisposeWriters();
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
