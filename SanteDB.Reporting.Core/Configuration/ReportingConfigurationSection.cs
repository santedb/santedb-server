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
using SanteDB.Core.Configuration;
using System;
using System.Xml.Serialization;

namespace SanteDB.Reporting.Core.Configuration
{
    /// <summary>
    /// Represents a configuration for a RISI configuration.
    /// </summary>
    [XmlType(nameof(ReportingConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class ReportingConfigurationSection : IConfigurationSection
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReportingConfigurationSection"/> class.
		/// </summary>
		public ReportingConfigurationSection()
		{
			
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReportingConfigurationSection"/> class
		/// with a specified report engine type.
		/// </summary>
		/// <param name="address">The address of the reporting engine.</param>
		/// <param name="handler">The type of report engine.</param>
		public ReportingConfigurationSection(string address, Type handler)
		{
			this.Address = address;
			this.Handler = handler;
		}

		/// <summary>
		/// Gets or sets the address of the reporting engine.
		/// </summary>
		[XmlAttribute("address")]
		public string Address { get; set; }

		/// <summary>
		/// Gets or sets the credentials.
		/// </summary>
		/// <value>The credentials.</value>
		[XmlElement("credentials")]
		public Credentials Credentials { get; set; }

		/// <summary>
		/// Gets the engine handler of the configuration.
		/// </summary>
		[XmlAttribute("type")]
        public String HandlerXml { get; set; }

        /// <summary>
        /// Gets the handler type
        /// </summary>
        [XmlIgnore]
        public Type Handler {
            get => Type.GetType(this.HandlerXml);
            set => this.HandlerXml = value?.AssemblyQualifiedName;
        }
	}
}