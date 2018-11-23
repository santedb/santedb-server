using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.AMI.Resources
{
    /// <summary>
    /// Represents a security application resource handler
    /// </summary>
    public class SecurityApplicationResourceHandler : SecurityEntityResourceHandler<SecurityApplication>
    {

        /// <summary>
        /// Get the type of results
        /// </summary>
        public override Type Type => typeof(SecurityApplicationInfo);
    }
}
