using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{
    /// <summary>
    /// Represents a single instance of security data on a path
    /// </summary>
    [JsonDictionary(nameof(SwaggerPathSecurity))]
    public class SwaggerPathSecurity : Dictionary<string, List<string>>
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public SwaggerPathSecurity()
        {

        }

        /// <summary>
        /// Copy ctor
        /// </summary>
        public SwaggerPathSecurity(IDictionary<string, List<string>> dictionary) : base(dictionary.ToDictionary(o=>o.Key, o=>new List<string>(o.Value)))
        {
        }
    }
}