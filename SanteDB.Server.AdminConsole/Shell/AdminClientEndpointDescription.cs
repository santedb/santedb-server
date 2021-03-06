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
using SanteDB.Core.Http.Description;
using System;

namespace SanteDB.Server.AdminConsole.Shell
{
    internal class AdminClientEndpointDescription : IRestClientEndpointDescription
    {

        /// <summary>
        /// Host of the endpoint
        /// </summary>
        public AdminClientEndpointDescription(String host)
        {
            this.Address = host;
            this.Timeout = 20000;
        }

        /// <summary>
        /// Gets or sets the address
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Gets or sets the timeout
        /// </summary>
        public int Timeout { get; set; }
    }
}