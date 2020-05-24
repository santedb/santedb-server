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
using RestSrvr.Attributes;
using SanteDB.Messaging.Metadata.Composer;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{
    /// <summary>
    /// Represents a swagger path definition
    /// </summary>
    [JsonObject(nameof(SwaggerPathDefinition))]
    public class SwaggerPathDefinition
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public SwaggerPathDefinition()
        {
            this.Tags = new List<string>();
            this.Produces = new List<string>();
            this.Consumes = new List<string>();
            this.Parameters = new List<SwaggerParameter>();
            this.Security = new List<SwaggerPathSecurity>();
            this.Responses = new Dictionary<int, SwaggerSchemaElement>();
        }

        /// <summary>
        /// Copy ctor
        /// </summary>
        public SwaggerPathDefinition(SwaggerPathDefinition copy)
        {
            this.Tags = new List<string>(copy.Tags);
            this.Produces = new List<string>(copy.Produces);
            this.Consumes = new List<string>(copy.Consumes);
            this.Parameters = copy.Parameters?.Select(o => new SwaggerParameter(o)).ToList();
            this.Security = copy.Security?.Select(o => new SwaggerPathSecurity(o)).ToList();
            this.Responses = copy.Responses.ToDictionary(o => o.Key, o => new SwaggerSchemaElement(o.Value));
            this.Summary = copy.Summary;
            this.Description = copy.Description;
        }

        /// <summary>
        /// Creates a new path definition 
        /// </summary>
        public SwaggerPathDefinition(MethodInfo behaviorMethod, MethodInfo contractMethod) : this()
        {
            var operationAtt = contractMethod.GetCustomAttribute<RestInvokeAttribute>();
            this.Summary = MetadataComposerUtil.GetElementDocumentation(behaviorMethod, MetaDataElementType.Summary) ?? MetadataComposerUtil.GetElementDocumentation(contractMethod, MetaDataElementType.Summary) ?? behaviorMethod.Name;
            this.Description = MetadataComposerUtil.GetElementDocumentation(behaviorMethod, MetaDataElementType.Remarks) ?? MetadataComposerUtil.GetElementDocumentation(contractMethod, MetaDataElementType.Remarks);
            this.Produces.AddRange(contractMethod.GetCustomAttributes<ServiceProducesAttribute>().Select(o => o.MimeType));
            this.Consumes.AddRange(contractMethod.GetCustomAttributes<ServiceConsumesAttribute>().Select(o => o.MimeType));

            // Obsolete?
            this.Obsolete = (behaviorMethod?.GetCustomAttribute<ObsoleteAttribute>() != null || contractMethod?.GetCustomAttribute<ObsoleteAttribute>() != null ||
                behaviorMethod.DeclaringType.GetCustomAttribute<ObsoleteAttribute>() != null || contractMethod.DeclaringType.GetCustomAttribute<ObsoleteAttribute>() != null);

            var parms = behaviorMethod.GetParameters();
            if (parms.Length > 0)
                this.Parameters = parms.Select(o => new SwaggerParameter(behaviorMethod, o, operationAtt)).ToList();

            var demands = behaviorMethod.GetCustomAttributes<DemandAttribute>();
            if (demands.Count() > 0)
                this.Security = new List<SwaggerPathSecurity>()
                {
                        new SwaggerPathSecurity()
                        {
                            { "oauth_user", demands.Select(o=>o.PolicyId).ToList() }
                        }
                };

            // Return type is not void
            if (behaviorMethod.ReturnType != typeof(void))
            {
                SwaggerSchemaElementType type = SwaggerSchemaElementType.@object;

                if (SwaggerSchemaElement.m_typeMap.TryGetValue(behaviorMethod.ReturnType, out type))
                    this.Responses.Add(200, new SwaggerSchemaElement()
                    {
                        Type = SwaggerSchemaElementType.@object,
                        Description = "Operation was completed successfully"
                    });
                else
                {
                    // Get the response type name
                    this.Responses.Add(200, new SwaggerSchemaElement()
                    {
                        Type = SwaggerSchemaElementType.@object,
                        Description = "Operation was completed successfully",
                        Schema = new SwaggerSchemaDefinition()
                        {
                            NetType = behaviorMethod.ReturnType,
                            Reference = $"#/definitions/{MetadataComposerUtil.CreateSchemaReference(behaviorMethod.ReturnType)}"
                        }
                    });
                }
            }
            else
                this.Responses.Add(204, new SwaggerSchemaElement()
                {
                    Description = "There is not response for this method"
                });

            // Any faults?
            foreach (var flt in contractMethod.GetCustomAttributes<ServiceFaultAttribute>())
                this.Responses.Add(flt.StatusCode, new SwaggerSchemaElement()
                {
                    Description = flt.Condition,
                    Schema = new SwaggerSchemaDefinition()
                    {
                        NetType = flt.FaultType,
                        Reference = $"#/definitions/{MetadataComposerUtil.CreateSchemaReference(flt.FaultType)}"
                    }
                });
        }


        /// <summary>
        /// Gets or sets the tags
        /// </summary>
        [JsonProperty("tags")]
        public List<String> Tags { get; set; }

        /// <summary>
        /// Gets or sets a summary description
        /// </summary>
        [JsonProperty("summary")]
        public String Summary { get; set; }

        /// <summary>
        /// Gets or sets the long form description
        /// </summary>
        [JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets or sets the produces option
        /// </summary>
        [JsonProperty("produces")]
        public List<String> Produces { get; set; }

        /// <summary>
        /// Gets or sets the consumption options
        /// </summary>
        [JsonProperty("consumes")]
        public List<String> Consumes { get; set; }

        /// <summary>
        /// Gets or sets the parameters 
        /// </summary>
        [JsonProperty("parameters")]
        public List<SwaggerParameter> Parameters { get; set; }

        /// <summary>
        /// Gets or sets the responses
        /// </summary>
        [JsonProperty("responses")]
        public Dictionary<Int32, SwaggerSchemaElement> Responses { get; set; }

        /// <summary>
        /// Gets or sets the security definition
        /// </summary>
        [JsonProperty("security")]
        public List<SwaggerPathSecurity> Security { get; set; }

        /// <summary>
        /// True if the method is deprecated
        /// </summary>
        [JsonProperty("deprecated")]
        public bool Obsolete { get; set; }

    }
}