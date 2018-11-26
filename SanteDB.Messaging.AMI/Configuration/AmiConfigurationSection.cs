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
 * Date: 2018-6-22
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Messaging.AMI.Configuration
{
    /// <summary>
    /// AMI Configuration
    /// </summary>
    [XmlType(nameof(AmiConfigurationSection), Namespace = "http://santedb.org/configuration/ami")]
    public class AmiConfigurationSection : IConfigurationSection
	{
        /// <summary>
        /// Ami configuration section
        /// </summary>
        public AmiConfigurationSection()
        {

        }

        /// <summary>
        /// Resources on the AMI that are forbidden
        /// </summary>
        [XmlIgnore]
        public IEnumerable<Type> ResourceHandlers
        {
            get
            {
                var msb = new ModelSerializationBinder();
                return this.ResourceHandlerXml.Select(o => msb.BindToType(null, o));
            }
        }

        /// <summary>
        /// Gets or sets the resource in xml format
        /// </summary>
        [XmlArray("resources"), XmlArrayItem("add")]
        public List<String> ResourceHandlerXml { get; set; }

        /// <summary>
        /// Certification authority configuration
        /// </summary>
        [XmlElement("msftCertAuth")]
        public CertificationAuthorityConfiguration CaConfiguration { get; private set; }

		/// <summary>
		/// Extra endpoints
		/// </summary>
        [XmlArray("endpoints"), XmlArrayItem("add")]
		public List<ServiceEndpointOptions> Endpoints { get; private set; }
	}
}