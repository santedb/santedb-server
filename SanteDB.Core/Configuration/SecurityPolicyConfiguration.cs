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
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SanteDB.Core.Configuration
{

    /// <summary>
    /// Policy identification
    /// </summary>
    [XmlType(nameof(SecurityPolicyIdentification), Namespace = "http://santedb.org/configuration")]
    public enum SecurityPolicyIdentification
    {
        /// <summary>
        /// Maximum password age
        /// </summary>
        [XmlEnum("auth.pwd.maxAge")]
        MaxPasswordAge,
        /// <summary>
        /// When changing a password question must be different
        /// </summary>
        [XmlEnum("auth.pwd.history")]
        PasswordHistory,
        /// <summary>
        /// Maximum invalid logins
        /// </summary>
        [XmlEnum("auth.failLogin")]
        MaxInvalidLogins,
        /// <summary>
        /// Maximum challenge age
        /// </summary>
        [XmlEnum("auth.challenge.maxAge")]
        MaxChallengeAge,
        /// <summary>
        /// When changing a challenge question must be different
        /// </summary>
        [XmlEnum("auth.challenge.history")]
        ChallengeHistory,
        /// <summary>
        /// Maximum length of sesison
        /// </summary>
        [XmlEnum("auth.session.length")]
        SessionLength,
        /// <summary>
        /// Maximum time to refresh session
        /// </summary>
        [XmlEnum("auth.session.refresh")]
        RefreshLength
    }

    /// <summary>
    /// Security policies
    /// </summary>
    [XmlType(nameof(SecurityPolicyConfiguration), Namespace = "http://santedb.org/configuration")]
    public class SecurityPolicyConfiguration
    {
        
        /// <summary>
        /// True if the policy is enabled
        /// </summary>
        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// The policy type
        /// </summary>
        [XmlAttribute("policy")]
        public SecurityPolicyIdentification PolicyId { get; set; }

        /// <summary>
        /// The policy value
        /// </summary>
        [XmlElement("int", typeof(Int32))]
        [XmlElement("timespan", typeof(TimeSpan))]
        [XmlElement("date", typeof(DateTime))]
        [XmlElement("list", typeof(List<String>))]
        [XmlElement("string", typeof(String))]
        [XmlElement("bool", typeof(Boolean))]
        [XmlElement("real", typeof(double))]
        public Object PolicyValue { get; set; }
    }
}