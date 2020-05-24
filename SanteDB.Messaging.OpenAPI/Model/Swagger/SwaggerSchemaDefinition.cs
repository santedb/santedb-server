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
using System.Collections.Generic;
using System;
using System.Linq;
using SanteDB.Messaging.Metadata.Composer;
using System.Reflection;
using SanteDB.Core.Model;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{
    /// <summary>
    /// Represents a swagger schema definition
    /// </summary>
    [JsonObject(nameof(SwaggerSchemaDefinition))]
    public class SwaggerSchemaDefinition
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public SwaggerSchemaDefinition()
        {
            this.Properties = new Dictionary<string, SwaggerSchemaElement>();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public SwaggerSchemaDefinition(SwaggerSchemaDefinition copy) : this()
        {
            this.Type = copy.Type;
            this.Reference = copy.Reference;
            this.Description = copy.Description;
            if (copy.Properties != null)
                this.Properties = copy.Properties.ToDictionary(o => o.Key, o => new SwaggerSchemaElement(o.Value));

            if (copy.AllOf != null)
                this.AllOf = copy.AllOf.Select(o => new SwaggerSchemaDefinition(o)).ToList();

            this.NetType = copy.NetType;
        }

        /// <summary>
        /// Create a schema definition based on a type
        /// </summary>
        public SwaggerSchemaDefinition(Type schemaType)
        {
            this.Description = MetadataComposerUtil.GetElementDocumentation(schemaType, MetaDataElementType.Summary);
            this.NetType = schemaType;
            this.Type = SwaggerSchemaElementType.@object;

            this.Properties = schemaType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttributes<XmlElementAttribute>().Any() || p.GetCustomAttribute<JsonPropertyAttribute>() != null)
                .ToDictionary(o => o.GetSerializationName(), o => new SwaggerSchemaElement(o));

            // XML info
            var xmlType = schemaType.GetCustomAttribute<XmlTypeAttribute>();
            if (xmlType != null)
                this.Xml = new SwaggerXmlInfo(xmlType);
        }

        /// <summary>
        /// Gets or set a reference to another object
        /// </summary>
        [JsonProperty("$ref")]
        public string Reference { get; set; }

        /// <summary>
        /// Gets or sets the xml information
        /// </summary>
        [JsonProperty("xml")]
        public SwaggerXmlInfo Xml { get; set; }

        /// <summary>
        /// Represents a property
        /// </summary>
        [JsonProperty("properties")]
        public Dictionary<String, SwaggerSchemaElement> Properties { get; set; }

        /// <summary>
        /// Represents an all-of relationship
        /// </summary>
        [JsonProperty("allOf")]
        public List<SwaggerSchemaDefinition> AllOf { get; set; }

        /// <summary>
        /// Gets the .net datatype
        /// </summary>
        [JsonIgnore]
        public Type NetType { get; set; }

        /// <summary>
        /// Gets the description of the datatype
        /// </summary>
        [JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets the type
        /// </summary>
        [JsonProperty("type")]
        public SwaggerSchemaElementType Type { get; internal set; }
    }
}