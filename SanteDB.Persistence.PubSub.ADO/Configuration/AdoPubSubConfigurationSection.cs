using SanteDB.Core.Configuration;
using SanteDB.OrmLite.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Persistence.PubSub.ADO.Configuration
{
    /// <summary>
    /// PubSub configuration section
    /// </summary>
    [XmlType(nameof(AdoPubSubConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class AdoPubSubConfigurationSection : OrmConfigurationBase, IConfigurationSection
    {
    }
}
