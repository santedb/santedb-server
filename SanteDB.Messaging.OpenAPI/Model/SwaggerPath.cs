using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace SanteDB.Messaging.OpenAPI.Model
{
    /// <summary>
    /// Represents a swagger path
    /// </summary>
    [JsonObject(nameof(SwaggerPath))]
    public class SwaggerPath : Dictionary<String, SwaggerPathDefinition>
    {
    }
}