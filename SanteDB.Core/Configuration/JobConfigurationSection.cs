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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// Represents a simple job configuration
    /// </summary>
    [XmlType(nameof(JobConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class JobConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Add job
        /// </summary>
        [XmlArray("jobs"), XmlArrayItem("add")]
        public List<JobItemConfiguration> Jobs { get; set; }

    }

    /// <summary>
    /// Represents the configuration of a single job
    /// </summary>
    public class JobItemConfiguration
    {

        /// <summary>
        /// The type as expressed in XML
        /// </summary>
        [XmlAttribute("type")]
        public String TypeXml { get; set; }

        /// <summary>
        /// Gets or sets the job type
        /// </summary>
        [XmlIgnore]
        public Type Type {
            get => !String.IsNullOrEmpty(this.TypeXml) ? Type.GetType(this.TypeXml) : null;
            set => this.TypeXml = value?.AssemblyQualifiedName;
        }

        /// <summary>
        /// Gets or sets the timeout of the job
        /// </summary>
        [XmlAttribute("interval")]
        public int Interval { get; set; }
    }
}
