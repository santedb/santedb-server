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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using System.Xml.Serialization;

namespace SanteDB.Server.Core.Configuration
{
    /// <summary>
    /// Represents a configuration section for file system queueing
    /// </summary>
    [XmlType(nameof(FileSystemQueueConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class FileSystemQueueConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Gets or sets the path to the queue location
        /// </summary>
        [XmlAttribute("queueRoot")]
        [Description("Identifies where file system queues should be created")]
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        
        //[Editor("System.Windows.Forms.Design.FolderNameEditor, System.Design, Version=4.0.0.0", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0")]
        public String QueuePath { get; set; }

    }
}
