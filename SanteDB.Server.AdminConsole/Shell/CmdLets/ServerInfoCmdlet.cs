/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core.Interop;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Server.AdminConsole.Attributes;
using SanteDB.Server.AdminConsole.Util;
using System;
using System.ComponentModel;

namespace SanteDB.Server.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Represents the server information commandlet
    /// </summary>
    [AdminCommandlet]
    public static class ServerInfoCmdlet
    {

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));

        /// <summary>
        /// Get server information
        /// </summary>
        public static void Init()
        {
            try
            {
                var diagReport = m_client.GetServerDiagnoticReport().ApplicationInfo;
                Console.WriteLine("* {0} -> v.{1} ({2})", m_client.Client.Description.Endpoint[0].Address, diagReport.Version, diagReport.InformationalVersion);
            }
            catch { }
        }


        /// <summary>
        /// Get diagnostic info from server
        /// </summary>
        [AdminCommand("server.info", "Gets diagnostic information from the server")]
        public static void ServerVersionQuery()
        {
            var diagReport = m_client.GetServerDiagnoticReport().ApplicationInfo;

            Console.WriteLine("Diagnostic Report for {0}", m_client.Client.Description.Endpoint[0].Address);
            Console.WriteLine("Server Reports As:\r\n {0} v. {2} ({3}) \r\n {4}", diagReport.Name, diagReport.Product, diagReport.Version, diagReport.InformationalVersion, diagReport.Copyright);
        }

        /// <summary>
        /// Get assembly info from server
        /// </summary>
        [AdminCommand("server.asm", "Shows the server assembly information")]
        public static void ServerAssemblyQuery()
        {
            var diagReport = m_client.GetServerDiagnoticReport().ApplicationInfo;

            // Loaded assemblies
            Console.WriteLine("Assemblies:\r\nAssembly{0}Version    Information", new String(' ', 22));
            foreach(var itm in diagReport.Assemblies)
            {
                if (itm.Name == "Microsoft.GeneratedCode") continue;
                Console.WriteLine("{0}{1}{2}{3}{4}",
                    itm.Name.Length > 28 ? itm.Name.Substring(0, 28) : itm.Name,
                    itm.Name.Length > 28 ? "  " : new string(' ', 30 - itm.Name.Length),
                    itm.Version.Length > 10 ? itm.Version.Substring(0, 10) : itm.Version,
                    itm.Version.Length > 10 ? " " : new string(' ', 11 - itm.Version.Length),
                    itm.Info?.Length > 50 ? itm.Info?.Substring(0, 50) : itm.Info);
            }
        }

        /// <summary>
        /// Get thread information
        /// </summary>
        [AdminCommand("server.threads", "Shows the server thread information")]
        [Description("This command will show the running threads in the connected IMS instance")]
        public static void ServiceThreadInformation ()
        {
            var diagReport = m_client.GetServerDiagnoticReport();
            DisplayUtil.TablePrint(diagReport.Threads,
                new String[] { "Name", "CPU Time", "State", "Task" },
                new int[] { 32, 10, 10, 32 },
                o => o.Name,
                o =>o.CpuTime,
                o => o.State,
                o => o.TaskInfo
            );
        }

        /// <summary>
        /// Get assembly info from server
        /// </summary>
        [AdminCommand("server.services", "Shows the server service information")]
        [Description("This command will show the running daemon services in the connected IMS instance")]
        public static void ServiceInformation()
        {
            var diagReport = m_client.GetServerDiagnoticReport().ApplicationInfo;

            DisplayUtil.TablePrint(diagReport.ServiceInfo,
                new String[] { "Service", "Classification", "Running" },
                new int[] { 60, 20, 10 },
                o => o.Description,
                o=> o.Class,
                o => o.IsRunning
            );
        }

    }
}
