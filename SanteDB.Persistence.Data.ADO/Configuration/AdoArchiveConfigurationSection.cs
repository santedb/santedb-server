using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Persistence.Data.ADO.Configuration
{
    /// <summary>
    /// Configuration for the archive configuration
    /// </summary>
    [XmlType(nameof(AdoArchiveConfigurationSection), Namespace = "http://santedb.org/configuration")]
    public class AdoArchiveConfigurationSection : AdoPersistenceConfigurationSection
    {
    }
}
