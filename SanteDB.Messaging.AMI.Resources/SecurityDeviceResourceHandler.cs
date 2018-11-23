using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler that handles security device operations
    /// </summary>
    public class SecurityDeviceResourceHandler : SecurityEntityResourceHandler<SecurityDevice>
    {
        /// <summary>
        /// Type of security device
        /// </summary>
        public override Type Type => typeof(SecurityDeviceInfo);
    }
}
