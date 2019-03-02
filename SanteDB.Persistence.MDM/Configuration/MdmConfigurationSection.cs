﻿/*
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
 * Date: 2018-9-25
 */
using SanteDB.Core.Configuration;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Persistence.MDM.Configuration
{
    /// <summary>
    /// Represents a configuration for MDM
    /// </summary>
    [XmlType(nameof(MdmConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class MdmConfigurationSection : IConfigurationSection
    {
        /// <summary>
        /// MDM configuration
        /// </summary>
        public MdmConfigurationSection()
        {

        }
        
        /// <summary>
        /// Gets or sets the resource types
        /// </summary>
        [XmlArray("resources"), XmlArrayItem("add")]
        public List<MdmResourceConfiguration> ResourceTypes { get; set; }

    }
}