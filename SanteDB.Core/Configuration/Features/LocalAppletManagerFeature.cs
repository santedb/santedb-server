/*
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
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Services.Impl;
using SanteDB.Server.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Core.Configuration.Features
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
            
            return base.CreateInstallTasks();
        }
        /// <summary>
        /// Auto-setup the applet features
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;

        /// <summary>
        /// Get the configuration type
        /// </summary>
        public override Type ConfigurationType => typeof(AppletConfigurationSection);

        /// <summary>
        /// Get default configuration
        /// </summary>
        protected override object GetDefaultConfiguration() => new AppletConfigurationSection();
    }
}
