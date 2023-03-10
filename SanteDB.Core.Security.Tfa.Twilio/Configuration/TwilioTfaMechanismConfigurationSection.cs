/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
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
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Serialization class
    public class TwilioTfaMechanismConfigurationSection : IConfigurationSection
    {
        /// <summary>
        /// Creates a new configuration instance for a Twilio account.
        /// </summary>
        public TwilioTfaMechanismConfigurationSection()
        {
        }

        /// <summary>
        /// Twilio Auth Token. This value is found in the Twilio Console under the Account Info section.
        /// </summary>
        [XmlAttribute("auth")]
        public String Auth { get; set; }

        /// <summary>
        /// E164 phone number to send text messages or place calls from.
        /// </summary>
        [XmlAttribute("from")]
        public String From { get; set; }

        /// <summary>
        /// Twilio Account SID. This value is found in the Twilio Console under the Account Info section.
        /// </summary>
        [XmlAttribute("sid")]
        public String Sid { get; set; }
    }
}