/*
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
using SanteDB.Core.Model.Serialization;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Persistence.MDM.Configuration
{
    /// <summary>
    /// Represents configuration for one resource
    /// </summary>
    [XmlType(nameof(MdmResourceConfiguration), Namespace = "http://santedb.org/configuration")]
    public class MdmResourceConfiguration
    {
        /// <summary>
        /// Serialization ctor
        /// </summary>
        public MdmResourceConfiguration()
        {

        }

        /// <summary>
        /// MDM resource configuration
        /// </summary>
        public MdmResourceConfiguration(Type type, String matchConfiguration, bool autoMerge)
        {
            this.ResourceTypeXml = type.GetCustomAttribute<XmlRootAttribute>()?.ElementName;
            this.MatchConfiguration = matchConfiguration;
            this.AutoMerge = autoMerge;
        }
        /// <summary>
        /// Gets or sets the resource type
        /// </summary>
        [XmlAttribute("type")]
        public String ResourceTypeXml { get; set; }

        /// <summary>
        /// Gets the resource
        /// </summary>
        [XmlIgnore]
        public Type ResourceType => new ModelSerializationBinder().BindToType(null, this.ResourceTypeXml);

        /// <summary>
        /// Gets or sets the match configuration
        /// </summary>
        [XmlAttribute("matchConfiguration")]
        public String MatchConfiguration { get; set; }

        /// <summary>
        /// Gets the auto merge attribute
        /// </summary>
        [XmlAttribute("autoMerge")]
        public bool AutoMerge { get; set; }
    }
}
