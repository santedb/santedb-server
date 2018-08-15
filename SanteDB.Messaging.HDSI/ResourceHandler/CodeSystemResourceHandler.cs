using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
    /// <summary>
    /// Represents a resource handler for code systems
    /// </summary>
    public class CodeSystemResourceHandler : ResourceHandlerBase<CodeSystem>
    {
        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        /// <summary>
        /// Create, update and delete require administer concept dictionary 
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.AdministerConceptDictionary)]
        public override object Obsolete(object key)
        {
            return base.Obsolete(key);
        }
    }
}
