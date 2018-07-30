using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents an object that provide a wrapper for a security info
    /// </summary>
    public interface ISecurityEntityInfo<TSecurityObject> : IAmiIdentified
        where TSecurityObject : SecurityEntity
    {

        /// <summary>
        /// Gets or sets the security object entity
        /// </summary>
        TSecurityObject Entity { get; set; }

        /// <summary>
        /// Gets the policies for the object
        /// </summary>
        List<SecurityPolicyInfo> Policies { get; set;  }
    }
}
