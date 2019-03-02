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
 * Date: 2018-6-22
 */
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Caching.Redis.Configuration
{
    /// <summary>
    /// Represents a simple redis configuration
    /// </summary>
    [XmlType(nameof(RedisConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class RedisConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Gets the configuration connection string
        /// </summary>
        [XmlArray("servers"), XmlArrayItem("add")]
        public List<String> Servers { get; set; }

        /// <summary>
        /// Username
        /// </summary>
        [XmlAttribute("username")]
        public String UserName { get; set; }

        /// <summary>
        /// Password to the server
        /// </summary>
        [XmlAttribute("password")]
        public String Password { get; set; }
    }
}