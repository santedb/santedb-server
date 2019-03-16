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
