/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2021-8-27
 */
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Server.Core.Configuration
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
    /// Gets the policy value as a timespan
    /// </summary>
    public class PolicyValueTimeSpan
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public PolicyValueTimeSpan()
        {

        }

        /// <summary>
        /// Timepsan value
        /// </summary>
        public PolicyValueTimeSpan(TimeSpan ts)
        {
            this.Value = ts;
        }

        /// <summary>
        /// Policy value timespan
        /// </summary>
        public PolicyValueTimeSpan(int hours, int minutes, int seconds)
        {
            this.Value = new TimeSpan(hours, minutes, seconds);
        }

        /// <summary>
        /// Time to live for XML serialization
        /// </summary>
        [XmlText]
        public string ValueXml
        {
            get { return XmlConvert.ToString(this.Value); }
            set { if (!string.IsNullOrEmpty(value)) this.Value = XmlConvert.ToTimeSpan(value); }
        }

        /// <summary>
        /// Gets or sets the value of the timespan
        /// </summary>
        [XmlIgnore]
        public TimeSpan Value { get; set; }

        /// <summary>
        /// Convert this wrapper to timespan
        /// </summary>
        public static explicit operator TimeSpan(PolicyValueTimeSpan instance) {
            return instance.Value;
        }

        /// <summary>
        /// Convert this wrapper to timespan
        /// </summary>
        public static explicit operator PolicyValueTimeSpan(TimeSpan instance)
        {
            return new PolicyValueTimeSpan() { Value = instance };
        }

        /// <summary>
        /// Represent as value
        /// </summary>
        public override string ToString() => this.ValueXml;

    }

    /// <summary>
    /// Security policies
    /// </summary>
    [XmlType(nameof(SecurityPolicyConfiguration), Namespace = "http://santedb.org/configuration")]
    public class SecurityPolicyConfiguration
    {

        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public SecurityPolicyConfiguration()
        {

        }

        /// <summary>
        /// Policy configuration ctor
        /// </summary>
        public SecurityPolicyConfiguration(SecurityPolicyIdentification policy, object value)
        {
            this.PolicyId = policy;
            this.PolicyValue = value;
            this.Enabled = true;
        }

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
        [XmlElement("timespan", typeof(PolicyValueTimeSpan))]
        [XmlElement("date", typeof(DateTime))]
        [XmlElement("list", typeof(List<String>))]
        [XmlElement("string", typeof(String))]
        [XmlElement("bool", typeof(Boolean))]
        [XmlElement("real", typeof(double))]
        public Object PolicyValue { get; set; }

        /// <summary>
        /// Convert to string
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{this.PolicyId}={this.PolicyValue}";

    }
}