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
 * Date: 2018-10-24
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Tools.AdminConsole.Attributes;
using SanteDB.Tools.AdminConsole.Util;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SanteDB.Tools.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Applet commands
    /// </summary>
    [AdminCommandlet]
    public static class AppletCmdlet
    {

        /// <summary>
        /// Represents an applet parameter
        /// </summary>
        public class AppletParameter
        {

            /// <summary>
            /// Applet identifier
            /// </summary>
            [Parameter("*")]
            [Description("The identifier for the applet to stat")]
            public StringCollection AppletId { get; set; }

        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));

        static AppletCmdlet()
        {
            m_client.Client.ProgressChanged += (o, e) =>
            {
                Console.CursorLeft = 1;
                Console.Write("{0:%}", e.Progress);
            };
        }

        /// <summary>
        /// List applets
        /// </summary>
        [AdminCommand("applet.list", "Lists all applets installed on the server")]
        public static void ListApplets()
        {
            var applets = m_client.GetApplets();
            DisplayUtil.TablePrint(applets.CollectionItem.OfType<AppletManifestInfo>(),
                new String[] { "ID", "Name", "Version", "Publisher", "S" },
                new int[] { 25, 25, 10, 58, 2 },
                o => o.AppletInfo.Id,
                o => o.AppletInfo.Names.FirstOrDefault().Value,
                o => o.AppletInfo.Version,
                o => o.AppletInfo.Author,
                o => o.PublisherData != null ? "*" : null
            );

        }

        /// <summary>
        /// Get a specific applet information
        /// </summary>
        [AdminCommand("applet.download", "Download the applet")]
        public static void GetApplet(AppletParameter parms)
        {
            foreach (var itm in parms.AppletId)
            {
                Console.Write("(    )   Downloading {0} > {0}.pak", itm);
                using (var rmtstream = m_client.DownloadApplet(itm))
                using (var stream = File.Create(itm + ".pak"))
                    rmtstream.CopyTo(stream);
                Console.CursorLeft = 1;
                Console.Write("100%");
                Console.WriteLine();
            }


        }
    }
}
