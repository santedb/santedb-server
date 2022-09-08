/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Data.Configuration
{

    /// <summary>
    /// The types of objects which should be cached
    /// </summary>
    [XmlType(nameof(AdoDataCachingPolicyTarget), Namespace = "http://santedb.org/configuration"), Flags]
    public enum AdoDataCachingPolicyTarget
    {
        /// <summary>
        /// Database objects should be cached
        /// </summary>
        [XmlEnum("db")]
        DatabaseObjects = 0x1,
        /// <summary>
        /// Model objects should be cached
        /// </summary>
        ModelObjects = 0x2,
        /// <summary>
        /// All objects should be cached
        /// </summary>
        AllObjects = DatabaseObjects | ModelObjects
    }

    /// <summary>
    /// Data caching policy
    /// </summary>
    [ExcludeFromCodeCoverage]
    [XmlType(nameof(AdoPersistenceCachingPolicy), Namespace = "http://santedb.org/configuration")]
    public class AdoPersistenceCachingPolicy
    {

        /// <summary>
        /// Gets or sets the targets
        /// </summary>
        [XmlElement("target")]
        public AdoDataCachingPolicyTarget Targets { get; set; }

        /// <summary>
        /// Type of object expiry
        /// </summary>
        [XmlElement("dataObjectExpiration")]
        public TimeSpan? DataObjectExpiry { get; set; }

    }
}