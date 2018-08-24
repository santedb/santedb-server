using SanteDB.Core.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SanteDB.Persistence.MDM.Configuration
{
    /// <summary>
    /// Configuration section handler
    /// </summary>
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Create MDM configuration object
        /// </summary>
        public object Create(object parent, object configContext, XmlNode section)
        {
            var resourceConfig = section.SelectNodes("./*[local-name() = 'resources']/*[local-name() = 'add']");
            List<MdmResourceConfiguration> resources = new List<MdmResourceConfiguration>(resourceConfig.Count);
            var binder = new ModelSerializationBinder();
            foreach (XmlElement rn in resourceConfig)
            {
                var type = binder.BindToType("SanteDB.Core.Model", rn.Attributes["resource"]?.Value);
                if (type == null)
                    throw new ConfigurationErrorsException($"Cannot find resource {rn.Attributes["resource"]?.Value}", rn);
                resources.Add(new MdmResourceConfiguration(type, rn.Attributes["matchConfiguration"]?.Value, Boolean.Parse(rn.Attributes["autoMerge"]?.Value ?? "true")));
            }
            return new MdmConfiguration(resources);
        }
    }
}
