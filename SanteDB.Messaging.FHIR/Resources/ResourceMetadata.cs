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
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Resource base
    /// </summary>
    [XmlType("Metadata", Namespace = "http://hl7.org/fhir")]
    public  class ResourceMetadata
    {
        /// <summary>
        /// Resource metadata ctor
        /// </summary>
        public ResourceMetadata()
        {
            this.Security = new List<FhirCoding>();
            this.Tags = new List<FhirCoding>();
        }

        /// <summary>
        /// Version id
        /// </summary>
        [XmlElement("versionId")]
        public FhirString VersionId { get; set; }

        /// <summary>
        /// Last update time
        /// </summary>
        [XmlElement("lastUpdated")]
        public FhirDateTime LastUpdated { get; set; }

        /// <summary>
        /// Profile id
        /// </summary>
        [XmlElement("profile")]
        public FhirUri Profile { get; set; }

        /// <summary>
        /// Security tags
        /// </summary>
        [XmlElement("security")]
        public List<FhirCoding> Security { get; set; }

        /// <summary>
        /// Tags 
        /// </summary>
        [XmlElement("tag")]
        public List<FhirCoding> Tags { get; set; }
    }
}