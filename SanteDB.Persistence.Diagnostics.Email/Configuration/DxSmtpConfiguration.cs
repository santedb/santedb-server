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
using System;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Diagnostics.Email.Configuration
{
    /// <summary>
    /// Configuration for SMTP
    /// </summary>
    [XmlType(nameof(DxSmtpConfiguration), Namespace = "http://santedb.org/configuration")]
    public class DxSmtpConfiguration
	{

        public DxSmtpConfiguration()
        {

        }
		/// <summary>
		/// SMTP configuration
		/// </summary>
		public DxSmtpConfiguration(Uri server, String userName, String password, bool ssl, String from)
		{
			this.ServerXml = server.ToString();
			this.Username = userName;
			this.Password = password;
			this.Ssl = ssl;
            this.From = from;
		}

        /// <summary>
        /// Gets the from address
        /// </summary>
        [XmlAttribute("from")]
        public string From { get; set; }

        /// <summary>
        /// Gets the password
        /// </summary>
        [XmlAttribute("password")]
        public string Password { get; set; }

		/// <summary>
		/// Gets the SMTP server
		/// </summary>
        [XmlAttribute("server")]
        public String ServerXml { get; set; }

        /// <summary>
        /// Gets the server
        /// </summary>
        [XmlIgnore]
        public Uri Server => new Uri(this.ServerXml);

        /// <summary>
        /// Get the SSL setting
        /// </summary>
        [XmlAttribute("ssl")]
        public bool Ssl { get; set; }

		/// <summary>
		/// Gets the username for connecting to the server
		/// </summary>
        [XmlAttribute("username")]
		public string Username { get; set; }
	}
}