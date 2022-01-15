using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Server.Core.Configuration
{
    /// <summary>
    /// File system queue configuration section
    /// </summary>
    [Obsolete("Use SanteDB.Core.Configuration.FileSystemQueueConfigurationSection", true)]
    public class FileSystemQueueConfigurationSection : SanteDB.Core.Configuration.FileSystemDispatcherQueueConfigurationSection
    {
    }
}