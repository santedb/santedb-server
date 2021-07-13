﻿/*
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
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Services;
using SanteDB.OrmLite.Configuration;
using SanteDB.OrmLite.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Data.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    [XmlType(nameof(AdoPersistenceConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class AdoPersistenceConfigurationSection : OrmConfigurationBase, IConfigurationSection
    {

        // Random
        private Random m_random = new Random();

        // Data provider
        private IDbProvider m_dbp;

        // PEPPER CHARS DEFAULT
        private const string PEPPER_CHARS = "0ABcDEfgILqZ~k";

        /// <summary>
        /// ADO configuration
        /// </summary>
        public AdoPersistenceConfigurationSection()
        {
            this.Validation = new EntityValidationFlags()
            {
                IdentifierFormat = true,
                IdentifierUniqueness = true,
                ValidationLevel = Core.BusinessRules.DetectedIssuePriorityType.Warning
            };
        }

        /// <summary>
        /// The persistence layer is not versioned
        /// </summary>
        [XmlAttribute("unversioned")]
        [Category("Behavior")]
        [DisplayName("Disable Versioning")]
        [Description("When enabled, turns off versioning in the persistence layer")]
        public bool Unversioned { get; set; }

        /// <summary>
        /// Gets or sets whether fuzzy totals should be used
        /// </summary>
        [XmlAttribute("fuzzyTotal")]
        [Category("Performance")]
        [DisplayName("Use Approx. Totals")]
        [Description("When set to true, the totalResults will not be an exact count, rather an indicator whether additional results are available (this does increase query performance but some clients may not be compatible with it)")]
        public bool UseFuzzyTotals { get; set; }

        /// <summary>
        /// The peppering characters for authentication hashes
        /// </summary>
        [XmlAttribute("pepper")]
        [Category("Security")]
        [DisplayName("Password peppering characters")]
        [Description("When set, identifies the pepper characters to use for authentication")]
        public String Pepper { get; set; }

        /// <summary>
        /// Maximum requests
        /// </summary>
        [XmlAttribute("maxRequests")]
        [Category("Performance")]
        [DisplayName("Request Throttling")]
        [Description("When set, instructs the ADO.NET data provider to limit queries")]
        public int MaxRequests { get; set; }

        /// <summary>
        /// When true, indicates that inserts can allow keyed inserts
        /// </summary>
        [XmlAttribute("autoUpdateExisting")]
        [Category("Behavior")]
        [DisplayName("Auto-Update Existing Resource")]
        [Description("When set, instructs the provider to automatically update existing records when Insert() is called")]
        public bool AutoUpdateExisting { get; set; }

        /// <summary>
        /// When true, indicates that inserts can allow auto inserts of child properties
        /// </summary>
        [XmlAttribute("autoInsertChildren")]
        [Category("Behavior")]
        [DisplayName("Auto-Insert Child Objects")]
        [Description("When set, instructs the provider to automatically insert any child objects to ensure integrity of the object")]
        public bool AutoInsertChildren { get; set; }


        /// <summary>
        /// True if statements should be prepared
        /// </summary>
        [XmlAttribute("prepareStatements")]
        [Category("Performance")]
        [DisplayName("Prepare SQL Queries")]
        [Description("When true, instructs the provider to prepare statements and reuse them during a transaction")]
        public bool PrepareStatements { get; set; }

        /// <summary>
        /// Validation flags
        /// </summary>
        [XmlElement("validation"), Category("Data Quality"), DisplayName("Validation"), Description("When set, enables data validation parameters")]
        public EntityValidationFlags Validation { get; set; }

        /// <summary>
        /// Max page size
        /// </summary>
        [XmlElement("maxPageSize")]
        public int? MaxPageSize { get;  set; }

        /// <summary>
        /// Identiifes the caching policy
        /// </summary>
        [XmlElement("caching"), Category("performance"), DisplayName("Caching Policy"), Description("Identifies the data caching policy for the database layer")]
        public AdoPersistenceCachingPolicy CachingPolicy { get; set; }

        /// <summary>
        /// Get all peppered combinations of the specified secret
        /// </summary>
        public IEnumerable<String> GetPepperCombos(String secret)
        {
            if(String.IsNullOrEmpty(this.Pepper))
            {
                return new String[] { secret }.Union(PEPPER_CHARS.Select(p => $"{secret}{p}"));
            }
            else
            {
                return new String[] { secret }.Union(this.Pepper.Select(p => $"{secret}{p}"));
            }
        }

        /// <summary>
        /// Adds pepper to the secret
        /// </summary>
        public String AddPepper(String secret)
        {
            if (String.IsNullOrEmpty(this.Pepper))
            {
                return $"{secret}{PEPPER_CHARS[m_random.Next(PEPPER_CHARS.Length - 1)]}";
            }
            else
            {
                return $"{secret}{this.Pepper[m_random.Next(this.Pepper.Length - 1)]}";
            }
        }
    }
}
