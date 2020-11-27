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
using Newtonsoft.Json;
using RestSrvr;
using SanteDB.Configuration.Attributes;
using SanteDB.Configuration.Converters;
using SanteDB.Configuration.Editors;
using System;
using System.ComponentModel;
using System.Drawing.Design;
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
        /// <summary>
        /// Creates a new rest behavior configuration
        /// </summary>
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
        [XmlAttribute("type"), JsonProperty("type"), Browsable(false)]
        public string XmlType { get; set; }

        /// <summary>
        /// Gets the type of the binding
        /// </summary>
        [XmlIgnore, JsonIgnore]
        public virtual Type Type
        {
            get
            {
                if (this.XmlType != null)
                    return Type.GetType(this.XmlType);
                else
                    return null;
            }
            set
            {
                this.XmlType = value?.AssemblyQualifiedName;
            }
        }

        /// <summary>
        /// Gets or sets the special configuration for the binding
        /// </summary>
        [XmlElement("configuration"), JsonProperty("configuration")]
        [DisplayName("Behavior Configuration"), Description("XML Configuration for the Behavior")]
        public XElement Configuration { get; set; }
    }

    /// <summary>
    /// Represents a single behavior configuration element with validation that the type is a IServiceBehavior
    /// </summary>
    [XmlType(nameof(RestServiceBehaviorConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    public class RestServiceBehaviorConfiguration : RestBehaviorConfiguration {

        /// <summary>
        /// Gets the type
        /// </summary>
        [Editor(typeof(TypeSelectorEditor), typeof(UITypeEditor)), TypeConverter(typeof(TypeDisplayConverter)), TypeSelectorBind(typeof(IServiceBehavior))]
        public override Type Type { get => base.Type; set => base.Type = value; }
    }

    /// <summary>
    /// Represents a single behavior configuration element with validation that the type is a IServiceBehavior
    /// </summary>
    [XmlType(nameof(RestEndpointBehaviorConfiguration), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    public class RestEndpointBehaviorConfiguration : RestBehaviorConfiguration
    {

        /// <summary>
        /// Gets the type
        /// </summary>
        [Editor(typeof(TypeSelectorEditor), typeof(UITypeEditor)), TypeConverter(typeof(TypeDisplayConverter)), TypeSelectorBind(typeof(IEndpointBehavior))]
        public override Type Type { get => base.Type; set => base.Type = value; }
    }
}