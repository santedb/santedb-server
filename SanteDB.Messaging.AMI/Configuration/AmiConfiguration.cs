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
using SanteDB.Core.Interop;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;

namespace SanteDB.Messaging.AMI.Configuration
{
	/// <summary>
	/// AMI Configuration
	/// </summary>
	public class AmiConfiguration
	{

        /// <summary>
        /// Creates a new AMI configuration
        /// </summary>
        public AmiConfiguration(CertificationAuthorityConfiguration caConfiguration, List<ServiceEndpointOptions> endpoints, List<Type> resourceHandler)
        {
            this.ResourceHandlers = resourceHandler;
            this.CaConfiguration = caConfiguration;
            this.Endpoints = endpoints;
        }

        /// <summary>
        /// Resources on the AMI that are forbidden
        /// </summary>
        public List<Type> ResourceHandlers { get; private set; }

        /// <summary>
        /// Certification authority configuration
        /// </summary>
        public CertificationAuthorityConfiguration CaConfiguration { get; private set; }

		/// <summary>
		/// Extra endpoints
		/// </summary>
		public List<ServiceEndpointOptions> Endpoints { get; private set; }
	}
}