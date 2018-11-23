/*
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
 * Date: 2018-9-25
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.MDM.Configuration
{
    /// <summary>
    /// Represents configuration for one resource
    /// </summary>
    public class MdmResourceConfiguration
    {

        /// <summary>
        /// Creates a new mdm resource configuration
        /// </summary>
        public MdmResourceConfiguration(Type resourceType, String matchConfiguration, bool merge)
        {
            this.ResourceType = resourceType;
            this.MatchConfiguration = matchConfiguration;
            this.AutoMerge = merge;
        }

        /// <summary>
        /// Gets or sets the resource type
        /// </summary>
        public Type ResourceType { get; private set; }

        /// <summary>
        /// Gets or sets the match configuration
        /// </summary>
        public String MatchConfiguration { get; private set; }

        /// <summary>
        /// Gets the auto merge attribute
        /// </summary>
        public bool AutoMerge { get; private set; }
    }
}
