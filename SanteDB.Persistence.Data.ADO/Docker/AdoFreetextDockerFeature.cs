using SanteDB.Core.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Persistence.Data.ADO.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Docker
{
    /// <summary>
    /// The ADO.NET freetext indexing docker feature
    /// </summary>
    public class AdoFreetextDockerFeature : IDockerFeature
    {
        /// <summary>
        /// Gets the identifier of this docker feature
        /// </summary>
        public string Id => "ADO_FTS";

        /// <summary>
        /// Gets the settings for this object
        /// </summary>
        public IEnumerable<string> Settings => new String[0];

        /// <summary>
        /// Configure the feature
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            serviceConfiguration.Add(new TypeReferenceConfiguration(typeof(AdoFreetextSearchService)));
        }
    }
}
