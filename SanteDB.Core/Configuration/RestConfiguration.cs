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
 * Date: 2018-11-23
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration
{
    /// <summary>
    /// REST configuration 
    /// </summary>
    public class RestConfiguration
    {

        /// <summary>
        /// Rest configuration
        /// </summary>
        public RestConfiguration()
        {
            this.Services = new List<RestServiceConfiguration>();
        }

        /// <summary>
        /// Gets the list of services
        /// </summary>
        public List<RestServiceConfiguration> Services { get; private set; }
    }

    public class RestServiceConfiguration
    {

        /// <summary>
        /// Creates a new rest service configuration
        /// </summary>
        public RestServiceConfiguration(String name)
        {
            this.Name = name;
            this.Endpoints = new List<RestEndpointConfiguration>();
            this.Behaviors = new List<Type>();
        }

        /// <summary>
        /// Gets or sets the name of the service
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// Gets or sets the endpoints
        /// </summary>
        public List<RestEndpointConfiguration> Endpoints { get; private set; }

        /// <summary>
        /// Gets or sets the behaviors
        /// </summary>
        public List<Type> Behaviors { get; private set; }

    }

    /// <summary>
    /// Represents a single endpoint configuration
    /// </summary>
    public class RestEndpointConfiguration
    {

        /// <summary>
        /// Creates a new REST endpoint
        /// </summary>
        public RestEndpointConfiguration(Uri address, Type contract)
        {
            this.Address = address;
            this.Contract = contract;
        }

        /// <summary>
        /// Gets or sets the listening address
        /// </summary>
        public Uri Address { get; private set; }

        /// <summary>
        /// Gets or sets the contract type
        /// </summary>
        public Type Contract { get; private set; }


    }
}
