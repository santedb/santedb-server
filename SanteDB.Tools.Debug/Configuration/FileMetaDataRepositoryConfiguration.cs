﻿/*
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Tools.Debug.Configuration
{
    /// <summary>
    /// File based metadata repository configuration
    /// </summary>
    [XmlType(nameof(FileMetaDataRepositoryConfiguration), Namespace = "http://santedb.org/configuration")]
    public class FileMetaDataRepositoryConfiguration
    {

        /// <summary>
        /// Metadata repository
        /// </summary>
        [XmlArray("paths")]
        [XmlArrayItem("add")]
        [Description("Sets the base directory for the BI repository")]
        public List<string> Paths { get; set; }

        /// <summary>
        /// Rescan time
        /// </summary>
        [XmlAttribute("rescan")]
        [Description("The time between scans on the repository ")]
        public int RescanTime { get; set; }

    }
}