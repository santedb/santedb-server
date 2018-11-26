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
 * Date: 2018-11-23
 */
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources.Attributes;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Resource base
    /// </summary>
    [XmlType("ResourceBase", Namespace = "http://hl7.org/fhir")]
    public class ResourceBase : FhirElement
    {
        
        /// <summary>
        /// ctor
        /// </summary>
        public ResourceBase()
        {
            this.Attributes = new List<ResourceAttributeBase>();
        }

        /// <summary>
        /// Gets or sets the internal identifier for the resource
        /// </summary>
        [XmlIgnore]
        public string Id { get; set; }

        /// <summary>
        /// Version identifier
        /// </summary>
        [XmlIgnore]
        public string VersionId
        {
            get { return this.Meta?.VersionId; }
            set
            {
                if (this.Meta == null) this.Meta = new ResourceMetadata();
                this.Meta.VersionId = value;
            }
        }

        /// <summary>
        /// Extended observations about the resource that can be used to tag the resource
        /// </summary>
        [XmlIgnore]
        public List<ResourceAttributeBase> Attributes { get; set; }

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        [XmlIgnore]
        public DateTime Timestamp
        {
            get { return this.Meta?.LastUpdated; }
            set
            {
                if (this.Meta == null) this.Meta = new ResourceMetadata();
                this.Meta.LastUpdated = value;
            }
        }

        /// <summary>
        /// Gets or sets the metadata
        /// </summary>
        [XmlElement("meta")]
        public ResourceMetadata Meta { get; set; }

    }
}
