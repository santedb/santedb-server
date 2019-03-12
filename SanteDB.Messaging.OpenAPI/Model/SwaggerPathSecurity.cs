using Newtonsoft.Json;
using System.Collections.Generic;

namespace SanteDB.Messaging.OpenAPI.Model
{
    /// <summary>
    /// Represents a single instance of security data on a path
    /// </summary>
    [JsonObject(nameof(SwaggerPathSecurity))]
    public class SwaggerPathSecurity : Dictionary<string, List<string>>
    {
    }
}