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
using SanteDB.Core.BusinessRules;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Data.ADO.Configuration
{
    /// <summary>
    /// Represents entity validation flags
    /// </summary>
    [XmlType(nameof(EntityValidationFlags), Namespace = "http://santedb.org/configuration")]
    public class EntityValidationFlags
    {

        /// <summary>
        /// Gets or sets whether identifier uniqueness should be validated
        /// </summary>
        [XmlAttribute("identifierUniqueness"), Description("When set, identifiers which are duplicate on domains set as unique will be rejected"), DisplayName("Identifier Uniqueness")]
        public bool IdentifierUniqueness { get; set; }

        /// <summary>
        /// Identifier format
        /// </summary>
        [XmlAttribute("identifierFormat"), Description("When set, identifiers which fail validation criteria will be rejected"), DisplayName("Identifier Format")]
        public bool IdentifierFormat { get; set; }

        /// <summary>
        /// When set don't reject registrations, but flag them
        /// </summary>
        [XmlAttribute("validationLevel"), Description("The default validation level"), DisplayName("Hard Validation")]
        public DetectedIssuePriorityType ValidationLevel { get; set; }
    }
}