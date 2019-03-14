using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.Metadata.Model.Swagger
{
    /// <summary>
    /// Represents a swagger path
    /// </summary>
    [JsonDictionary(nameof(SwaggerPath))]
    public class SwaggerPath : Dictionary<String, SwaggerPathDefinition>
    {

        /// <summary>
        /// Create a new swagger path
        /// </summary>
        public SwaggerPath()
        {

        }

        /// <summary>
        /// Create a copied swagger path
        /// </summary>
        public SwaggerPath(IDictionary<String, SwaggerPathDefinition> copy) : base(copy.ToDictionary(o=>o.Key, o=>new SwaggerPathDefinition(o.Value)))
        {

        }
    }
}
