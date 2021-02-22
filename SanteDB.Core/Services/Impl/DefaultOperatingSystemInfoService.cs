using SanteDB.Core;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Default operating system service
    /// </summary>
    public class DefaultOperatingSystemInfoService : IOperatingSystemInfoService
    {
        /// <summary>
        /// Gets the version
        /// </summary>
        public string VersionString => Environment.OSVersion.VersionString;

        /// <summary>
        /// Operating system id
        /// </summary>
        /// <summary>
        /// Get the operating system type
        /// </summary>
        public OperatingSystemID OperatingSystem
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.MacOSX:
                        return OperatingSystemID.MacOS;
                    case PlatformID.Unix:
                        return OperatingSystemID.Linux;
                    case PlatformID.Win32NT:
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.WinCE:
                    case PlatformID.Xbox:
                        return OperatingSystemID.Win32;
                    default:
                        throw new InvalidOperationException("Invalid platform");
                }
            }
        }

        /// <summary>
        /// Gets the machine name
        /// </summary>
        public string MachineName => Environment.MachineName;

        /// <summary>
        /// Manufacturer name
        /// </summary>
        public string ManufacturerName => "Generic Manufacturer";

    }
}
