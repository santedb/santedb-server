using SanteDB.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        /// File system queue feature
        /// </summary>
        public FileSystemQueueFeature()
        {
            this.Configuration = new FileSystemQueueConfigurationSection()
            {
                QueuePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "queue")
            };
        }

        /// <summary>
        /// Gets the group
        /// </summary>
        public override string Group => "Message Queue";

    }
}
