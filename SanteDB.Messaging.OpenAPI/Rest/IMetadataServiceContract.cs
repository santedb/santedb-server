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
using RestSrvr.Attributes;
using SanteDB.Messaging.Metadata.Model.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.Metadata.Rest
{
    /// <summary>
    /// Metadata Exchange Service
    /// </summary>
    [ServiceContract(Name = "Metadata Service")]
    public interface IMetadataServiceContract
    {

        /// <summary>
        /// Get OpenAPI Definitions
        /// </summary>
        /// <returns></returns>
        [Get("/swagger-config.json")]
        [ServiceProduces("application/json")]
        Stream GetOpenApiDefinitions();

        /// <summary>
        /// Gets the swagger document for the overall contract
        /// </summary>
        [Get("/{serviceName}/{composer}")]
        [ServiceProduces("application/json")]
        object GetMetadata(String serviceName, String composer);

        /// <summary>
        /// Gets the specified object
        /// </summary>
        [Get("/{*content}")]
        Stream Index(String content);
    }
}
