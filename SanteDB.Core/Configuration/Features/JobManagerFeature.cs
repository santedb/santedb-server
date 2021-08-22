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
 * Date: 2021-8-5
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Features;
using SanteDB.Core.Jobs;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Core.Configuration.Features
{
    /// <summary>
    /// Represents a feature for the job manager
    /// </summary>
    public class JobManagerFeature : GenericServiceFeature<DefaultJobManagerService>
    {

        /// <summary>
        /// Job manager feature ctor
        /// </summary>
        public JobManagerFeature()
        {
            this.Configuration = new JobConfigurationSection()
            {
                Jobs = new List<JobItemConfiguration>()
            };
        }

        /// <summary>
        /// Job Manager Configuration
        /// </summary>
        public override string Group => FeatureGroup.System;

        /// <summary>
        /// Setup the job manager
        /// </summary>
        public override FeatureFlags Flags => FeatureFlags.AutoSetup; 

        /// <summary>
        /// Gets the description
        /// </summary>
        public override string Description => "Allows SanteDB to run scheduled or ad-hoc 'jobs' (such as compression, warehousing, backup)";

        /// <summary>
        /// Gets the configuration type
        /// </summary>
        public override Type ConfigurationType => typeof(JobConfigurationSection);

    }
}
