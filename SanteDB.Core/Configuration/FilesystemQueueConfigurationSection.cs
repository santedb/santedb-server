﻿/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-3-2
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
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
        public String QueuePath { get; set; }

    }
}