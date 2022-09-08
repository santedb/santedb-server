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
 * Date: 2022-5-30
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.PubSub.ADO.Configuration
{
    /// <summary>
    /// Represents an ADO persistence service
    /// </summary>
    public class AdoPubSubFeature : GenericServiceFeature<AdoPubSubManager>
    {

        /// <summary>
        /// Set the default configuration
        /// </summary>
        public AdoPubSubFeature() : base()
        {
            
        }

        /// <summary>
        /// Gets the type of configuration section
        /// </summary>
        public override Type ConfigurationType => typeof(AdoPubSubConfigurationSection);

        /// <summary>
        /// Automatically setup
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup;

        /// <summary>
        /// Group for this setting
        /// </summary>
        public override string Group => FeatureGroup.Persistence;

        /// <summary>
        /// Get default configuration
        /// </summary>
        protected override object GetDefaultConfiguration() => new AdoPubSubConfigurationSection()
        {
            TraceSql = false
        };
    }
}
