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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Newtonsoft.Json;
using RestSrvr.Attributes;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Attributes;
using SanteDB.Messaging.Metadata.Composer;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{

    /// <summary>
    /// Represents the swagger parameter location
    /// </summary>
    public enum SwaggerParameterLocation
    {
        /// <summary>
        /// Location is in the body
        /// </summary>
        body, 
        /// <summary>
        /// Location is in the path
        /// </summary>
        path, 
        /// <summary>
        /// Location is in the query
        /// </summary>
        query
    }
    /// <summary>
    /// Represents the swagger parameter
    /// </summary>
    [JsonObject(nameof(SwaggerParameter))]
    public class SwaggerParameter : SwaggerSchemaElement
    {

       
        /// <summary>
        /// Constructor for serializer
        /// </summary>
        public SwaggerParameter()
        {

        }
        /// <summary>
        /// Create a swagger query parameter
        /// </summary>
        public SwaggerParameter(PropertyInfo queryFilter) 
        {

            this.Name = queryFilter.GetSerializationName() ?? queryFilter.GetCustomAttribute<QueryParameterAttribute>()?.ParameterName;
            this.Description = MetadataComposerUtil.GetElementDocumentation(queryFilter);
            this.Location = SwaggerParameterLocation.query;
            
            SwaggerSchemaElementType type = SwaggerSchemaElementType.@string;
            if (queryFilter.PropertyType.StripNullable().IsEnum)
            {
                this.Enum = queryFilter.PropertyType.StripNullable().GetFields().Select(f => f.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault()?.Name).Where(o => !string.IsNullOrEmpty(o)).ToList();
                this.Type = SwaggerSchemaElementType.@string;
            }
            else if (!m_typeMap.TryGetValue(queryFilter.PropertyType, out type))
                this.Type = SwaggerSchemaElementType.@string;
            else
                this.Type = type;

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public SwaggerParameter(SwaggerParameter copy) : base(copy)
        {
            this.Name = copy.Name;
            this.Location = copy.Location;
        }

        /// <summary>
        /// Creates a new swagger parameter
        /// </summary>
        public SwaggerParameter(MethodInfo method, ParameterInfo parameter, RestInvokeAttribute operation)
        {
            this.Name = parameter.Name;
            this.Location = operation.UriTemplate.Contains($"{{{parameter.Name}}}") ? SwaggerParameterLocation.path : SwaggerParameterLocation.body;
            this.Description = MetadataComposerUtil.GetElementDocumentation(method, parameter);

            SwaggerSchemaElementType type = SwaggerSchemaElementType.@string;
            if (parameter.ParameterType.StripNullable().IsEnum)
            {
                this.Enum = parameter.ParameterType.StripNullable().GetFields().Select(f => f.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault()?.Name).Where(o => !string.IsNullOrEmpty(o)).ToList();
                this.Type = SwaggerSchemaElementType.@string;
            }
            else if (!m_typeMap.TryGetValue(parameter.ParameterType, out type))
            {
                this.Schema = new SwaggerSchemaDefinition()
                {
                    Reference = $"#/definitions/{MetadataComposerUtil.CreateSchemaReference(parameter.ParameterType)}",
                    NetType = parameter.ParameterType
                };
            }
            else
                this.Type = type;
        }

        /// <summary>
        /// Gets or sets the name of the parameter
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets location of the parameter
        /// </summary>
        [JsonProperty("in")]
        public SwaggerParameterLocation Location { get; set; }


    }
}