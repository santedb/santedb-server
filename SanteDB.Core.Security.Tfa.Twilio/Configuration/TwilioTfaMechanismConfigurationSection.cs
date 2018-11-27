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
 * Date: 2018-6-22
 */
using SanteDB.Core.Configuration;
using System;
using System.Xml.Serialization;

namespace SanteDB.Core.Security.Tfa.Twilio.Configuration
{
    /// <summary>
    /// Represents the configuration for the TFA mecahnism
    /// </summary>
    [XmlType(nameof(TwilioTfaMechanismConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class TwilioTfaMechanismConfigurationSection : IConfigurationSection
	{
		/// <summary>
		/// Creates a new template mechanism configuration
		/// </summary>
		public TwilioTfaMechanismConfigurationSection()
		{
		}

		/// <summary>
		/// Authentication token
		/// </summary>
        [XmlAttribute("auth")]
		public String Auth { get; set; }

		/// <summary>
		/// From number
		/// </summary>
        [XmlAttribute("from")]
		public String From { get; set; }

		/// <summary>
		/// SID configuration
		/// </summary>
        [XmlAttribute("sid")]
		public String Sid { get; set; }
	}
}