/*
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
using SanteDB.Messaging.Metadata.Model.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.Metadata.Composer
{
    /// <summary>
    /// Represents a swagger 2.0 documentation composer
    /// </summary>
    public class SwaggerMetadataComposer : IMetadataComposer
    {

        // Cache
        private Dictionary<String, SwaggerDocument> m_cache = new Dictionary<string, SwaggerDocument>();

        /// <summary>
        /// Get the name of the documentation composer
        /// </summary>
        public string Name => "swagger.json";

        /// <summary>
        /// Compose documentation
        /// </summary>
        public object ComposeDocumentation(String serviceName)
        {
            SwaggerDocument retVal = null;
            if(!this.m_cache.TryGetValue(serviceName, out retVal))
            {
                var service = MetadataComposerUtil.ResolveService(serviceName);
                if (service == null)
                    throw new KeyNotFoundException($"Service {serviceName} not found");

                // Create swagger document
                retVal = new SwaggerDocument(service);

                lock (this.m_cache)
                    if(!this.m_cache.ContainsKey(serviceName))
                        this.m_cache.Add(serviceName, retVal);
            }
            return retVal;
        }
    }
}
