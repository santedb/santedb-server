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
using SanteDB.Core.Configuration;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Messaging.HDSI.Configuration
{
    /// <summary>
    /// Configuration class for HDSI configuration
    /// </summary>
    [XmlType(nameof(HdsiConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class HdsiConfigurationSection : IConfigurationSection
    {
        /// <summary>
        /// HDSI Configuration
        /// </summary>
        public HdsiConfigurationSection()
        {
            
        }

        /// <summary>
        /// Resources on the AMI that are forbidden
        /// </summary>
        [XmlIgnore, Browsable(false)]
        public IEnumerable<Type> ResourceHandlers
        {
            get
            {
                var msb = new ModelSerializationBinder();
                return this.ResourceHandlerXml?.Select(o => msb.BindToType(null, o));
            }
        }

        /// <summary>
        /// Gets or sets the resource in xml format
        /// </summary>
        [XmlArray("resources"), XmlArrayItem("add"), ReadOnly(true)]
        public List<String> ResourceHandlerXml { get; set; }

       
    }
}