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
    /// Represents a storage queue on the file system
    /// </summary>
    public class FileSystemQueueFeature : GenericServiceFeature<FileSystemQueueService>
    {

        /// <summary>
        /// File system queue feature
        /// </summary>
        public FileSystemQueueFeature()
        {
            this.Configuration = new FileSystemQueueConfigurationSection()
            {
                QueuePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "queue")
            };
        }

        /// <summary>
        /// Gets the group
        /// </summary>
        public override string Group => FeatureGroup.System;

        /// <summary>
        /// File system queue configuration service
        /// </summary>
        public override Type ConfigurationType => typeof(FileSystemQueueConfigurationSection);
    }
}
