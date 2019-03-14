using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.Metadata.Composer
{
    /// <summary>
    /// Represents a documentation composer
    /// </summary>
    public interface IMetadataComposer
    {

        /// <summary>
        /// Get the name of the composer
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Compose documentation 
        /// </summary>
        Object ComposeDocumentation(String serviceName);

    }
}
