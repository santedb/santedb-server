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
