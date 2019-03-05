/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using System.Xml.Serialization;

namespace SanteDB.Messaging.AMI.Configuration
{
    /// <summary>
    /// CA configuration information
    /// </summary>
    [XmlType(nameof(CertificationAuthorityConfiguration), Namespace = "http://santedb.org/configuration")]
    public class CertificationAuthorityConfiguration
	{
		/// <summary>
		/// When true, automatically approve CA
		/// </summary>
        [XmlAttribute("autoApprove")]
		public bool AutoApprove { get; set; }

		/// <summary>
		/// Gets or sets the name of the certification authority
		/// </summary>
        [XmlAttribute("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the name of the machine
		/// </summary>
        [XmlAttribute("server")]
		public string ServerName { get; set; }
	}
}