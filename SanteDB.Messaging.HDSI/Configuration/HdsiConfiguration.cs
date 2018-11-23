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
 * Date: 2018-6-22
 */
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;

namespace SanteDB.Messaging.HDSI.Configuration
{
    /// <summary>
    /// Configuration class for HDSI configuration
    /// </summary>
    public class HdsiConfiguration
    {

        /// <summary>
        /// Creates a new HDSI configuration
        /// </summary>
        public HdsiConfiguration(List<Type> resourceHandler)
        {
            this.ResourceHandlers = resourceHandler;
        }

      
        /// <summary>
        /// Gets or sets the resource tool that can be used for the configuration
        /// </summary>
        public List<Type> ResourceHandlers { get; private set; }


    }
}