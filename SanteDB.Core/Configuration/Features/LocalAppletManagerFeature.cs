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
using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration.Features
{
    /// <summary>
    /// Represents the local applet manager feature
    /// </summary>
    public class LocalAppletManagerFeature : GenericServiceFeature<LocalAppletManagerService>
    {

        /// <summary>
        /// Create a new local applet manager 
        /// </summary>
        public LocalAppletManagerFeature()
        {
            this.Configuration = new AppletConfigurationSection();
        }

        /// <summary>
        /// Create installation tasks
        /// </summary>
        public override IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            var conf = this.Configuration as AppletConfigurationSection ?? new AppletConfigurationSection()
            {
                AppletDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "applets")
            };
            if (conf.TrustedPublishers.Count == 0)
                conf.TrustedPublishers.AddRange(new String[]
                    {
                        "82C63E1E9B87578D0727E871D7613F2F0FAF683B", // SanteDB APPCA Signature (must be installed)
                        "4326A4421216AC254DA93DC61B93160B08925BB1" // SanteDB Community Applications
                    });
            return base.CreateInstallTasks();
        }
        /// <summary>
        /// Auto-setup the applet features
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;
    }
}
