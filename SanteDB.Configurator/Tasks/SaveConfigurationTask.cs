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
using SanteDB.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Configurator.Tasks
{
    /// <summary>
    /// Represents a configuration task
    /// </summary>
    public class SaveConfigurationTask : IConfigurationTask
    {

        // The name of the backup file
        private String m_backupFile;

        /// <summary>
        /// Save the configuration 
        /// </summary>
        public string Name => "Save SanteDB Configuration";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Writes the changes made to the configuration file to {ConfigurationContext.Current.ConfigurationFile}";

        /// <summary>
        /// Gets the feature
        /// </summary>
        public IFeature Feature => ConfigurationContext.Current.Features.OfType<CoreServiceFeatures>().First();

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the configuration
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            // First we backup the configuration
            this.m_backupFile = $"{ConfigurationContext.Current.ConfigurationFile}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.bak";
            if(File.Exists(ConfigurationContext.Current.ConfigurationFile))
                File.Copy(ConfigurationContext.Current.ConfigurationFile, this.m_backupFile, true);

            configuration.Includes = null;
            using (var fs = File.Create(ConfigurationContext.Current.ConfigurationFile))
                configuration.Save(fs);
            return true;
        }

        /// <summary>
        /// Rollback the changes
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            if (File.Exists(this.m_backupFile))
            {
                File.Copy(this.m_backupFile, ConfigurationContext.Current.ConfigurationFile);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Verify that this task can run
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration)
        {
            return true;
        }
    }
}
