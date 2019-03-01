using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Configuration.Features
{
    /// <summary>
    /// Represents a storage queue on the file system
    /// </summary>
    public class FileSystemQueueFeature : GenericServiceFeature<FileSystemQueueService>
    {

        /// <summary>
        /// Gets the group
        /// </summary>
        public override string Group => "Message Queue";

    }
}
