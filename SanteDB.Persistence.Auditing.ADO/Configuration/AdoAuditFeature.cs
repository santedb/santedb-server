﻿/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-10
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Persistence.Auditing.ADO.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Auditing.ADO.Configuration
{
    /// <summary>
    /// ADO.NET Auditing Feature
    /// </summary>
    public class AdoAuditFeature : GenericServiceFeature<AdoAuditRepositoryService>
    {

        /// <summary>
        /// Creates a new ado audit feature
        /// </summary>
        public AdoAuditFeature()
        {
            this.Configuration = new AdoAuditConfigurationSection()
            {
                TraceSql = false
            };
        }

        /// <summary>
        /// Persistence feature
        /// </summary>
        public override string Group => FeatureGroup.Persistence;

        /// <summary>
        /// Flags for this feature
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;

        /// <summary>
        /// Gets the type of configuration
        /// </summary>
        public override Type ConfigurationType => typeof(AdoAuditConfigurationSection);
    }
}
