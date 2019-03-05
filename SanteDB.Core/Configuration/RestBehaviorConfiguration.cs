/*
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
 * Date: 2019-1-22
 */
using Newtonsoft.Json;
using System;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Represents a single behavior configuration element
    /// </summary>
    [XmlType(nameof(RestBehaviorConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    public class RestBehaviorConfiguration
    {

        public RestBehaviorConfiguration()
        {

        }

        /// <summary>
        /// AGS Behavior Configuration
        /// </summary>
        public RestBehaviorConfiguration(Type behaviorType)
        {
            this.Type = behaviorType;
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [XmlAttribute("type"), JsonProperty("type")]
        public string XmlType { get; set; }

        /// <summary>
        /// Gets the type of the binding
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public Type Type
        {
            get
            {
                return Type.GetType(this.XmlType);
            }
            set
            {
                this.XmlType = value.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets or sets the special configuration for the binding
        /// </summary>
        [XmlElement("configuration"), JsonProperty("configuration")]
        public XElement Configuration { get; set; }
    }
}