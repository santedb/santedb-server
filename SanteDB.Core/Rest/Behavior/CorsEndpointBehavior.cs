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
 * Date: 2018-11-23
 */
using RestSrvr;
using SanteDB.Core.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Rest.Behavior
{
    /// <summary>
    /// Adds message CORS insepectors
    /// </summary>
    public class CorsEndpointBehavior : IEndpointBehavior
    {

        // Settings
        private CorsSettings m_settings;

        /// <summary>
        /// Creates a new CORS endpoint behavior
        /// </summary>
        public CorsEndpointBehavior(CorsSettings settings)
        {
            this.m_settings = settings;
        }

        /// <summary>
        /// Apply endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(new CorsMessageInspector(this.m_settings));
        }
    }
}