/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{
    [JsonObject(nameof(SwaggerServiceInfo))]
    public class SwaggerServiceInfo
    {

        /// <summary>
        /// Gets or sets the title of the API
        /// </summary>
        [JsonProperty("title")]
        public String Title { get; set; }

        /// <summary>
        /// Gets or sets the definition
        /// </summary>
        [JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets or sets the version
        /// </summary>
        [JsonProperty("version")]
        public String Version { get; set; }
    }
}