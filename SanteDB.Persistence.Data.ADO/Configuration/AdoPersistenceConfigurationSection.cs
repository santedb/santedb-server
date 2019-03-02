﻿/*
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
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Services;
using SanteDB.OrmLite.Configuration;
using SanteDB.OrmLite.Providers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Data.ADO.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    [XmlType(nameof(AdoPersistenceConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class AdoPersistenceConfigurationSection : OrmConfigurationBase, IConfigurationSection
    {
        /// <summary>
        /// Resolve the connection string
        /// </summary>
        protected override string ResolveConnectionString(string connectionStringName)
        {
            return ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetConnectionString(connectionStringName)?.Value;
        }

        // Data provider
        private IDbProvider m_dbp;

        /// <summary>
        /// ADO configuration
        /// </summary>
        public AdoPersistenceConfigurationSection()
        {
            this.DataCorrectionKeys = new List<string>();
        }

        /// <summary>
        /// Multi-threaded fetch
        /// </summary>
        [XmlAttribute("staOnly")]
        [Description("When set, instructs ADO.NET data fetches to be on a single thread")]
        public bool SingleThreadFetch { get; set; }

        /// <summary>
        /// Maximum requests
        /// </summary>
        [XmlAttribute("maxRequests")]
        [Description("When set, instructs the ADO.NET data provider to limit queries")]
        public int MaxRequests { get; set; }

        /// <summary>
        /// When true, indicates that inserts can allow keyed inserts
        /// </summary>
        [XmlAttribute("autoUpdateExisting")]
        [Description("When set, instructs the provider to automatically update existing records when Insert() is called")]
        public bool AutoUpdateExisting { get; set; }

        /// <summary>
        /// When true, indicates that inserts can allow auto inserts of child properties
        /// </summary>
        [XmlAttribute("autoInsertChildren")]
        [Description("When set, instructs the provider to automatically insert any child objects to ensure integrity of the object")]
        public bool AutoInsertChildren { get; set; }


        /// <summary>
        /// Gets a list of data corrections to apply
        /// </summary>
        [XmlArray("corrections"), XmlArrayItem("add")]
        [Description("Identifies the data patches to be executed")]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        public List<String> DataCorrectionKeys { get; set; }

        /// <summary>
        /// Allowed resources
        /// </summary>
        [XmlArray("resources"), XmlArrayItem("add")]
        [Description("When set, instructs the provider to only provide access for the specified types")]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof(UITypeEditor))]
        public List<String> AllowedResources { get; set; }

        /// <summary>
        /// True if statements should be prepared
        /// </summary>
        [XmlAttribute("prepareStatements")]
        [Description("When true, instructs the provider to prepare statements")]
        public bool PrepareStatements { get; set; }
    }
}