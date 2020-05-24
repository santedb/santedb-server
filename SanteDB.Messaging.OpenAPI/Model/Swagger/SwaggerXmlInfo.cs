/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{
    /// <summary>
    /// Represents swagger xml information 
    /// </summary>
    [JsonObject(nameof(SwaggerXmlInfo))]
    public class SwaggerXmlInfo
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public SwaggerXmlInfo()
        {

        }

        /// <summary>
        /// Creates a new xml structure from xml attribute
        /// </summary>
        public SwaggerXmlInfo(XmlTypeAttribute typeInfo)
        {
            this.Namespace = typeInfo.Namespace;
        }

        /// <summary>
        /// Creates a new xml structure from element
        /// </summary>
        public SwaggerXmlInfo(XmlElementAttribute elementInfo)
        {
            this.Name = elementInfo.ElementName;
            this.Namespace = elementInfo.Namespace;
        }

        /// <summary>
        /// Creates new xml structure from attribute info
        /// </summary>
        public SwaggerXmlInfo(XmlAttributeAttribute attributeInfo)
        {

            this.IsAttribute = true;
            this.Name = attributeInfo.AttributeName;
            this.Namespace = attributeInfo.Namespace;
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Attribute
        /// </summary>
        [JsonProperty("attribute")]
        public bool IsAttribute { get; set; }

        /// <summary>
        /// Gets or sets the namespace
        /// </summary>
        [JsonProperty("namespace")]
        public string Namespace { get; set; }


    }
}