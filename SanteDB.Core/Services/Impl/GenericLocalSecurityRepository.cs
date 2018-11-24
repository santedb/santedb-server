using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Generic local security repository
    /// </summary>
    public abstract class GenericLocalSecurityRepository<TSecurityEntity> : GenericLocalMetadataRepository<TSecurityEntity>, ISecurityAuditEventSource
        where TSecurityEntity : IdentifiedData
    {
        /// <summary>
        /// Fired when security attributes have changed
        /// </summary>
        public event EventHandler<SecurityAuditDataEventArgs> SecurityAttributesChanged;
        /// <summary>
        /// Fired when a security resource has been created
        /// </summary>
        public event EventHandler<SecurityAuditDataEventArgs> SecurityResourceCreated;
        /// <summary>
        /// Fired when a security resource has been deleted
        /// </summary>
        public event EventHandler<SecurityAuditDataEventArgs> SecurityResourceDeleted;


        /// <summary>
        /// Insert the object
        /// </summary>
        public override TSecurityEntity Insert(TSecurityEntity data)
        {
            var retVal = base.Insert(data);
            this.SecurityResourceCreated?.Invoke(this, new SecurityAuditDataEventArgs(retVal));
            return retVal;
        }

        /// <summary>
        /// Save the object
        /// </summary>
        public override TSecurityEntity Save(TSecurityEntity data)
        {
            var retVal = base.Save(data);
            this.SecurityAttributesChanged?.Invoke(this, new SecurityAuditDataEventArgs(retVal));
            return retVal;
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override TSecurityEntity Obsolete(Guid key)
        {
            var retVal = base.Obsolete(key);
            this.SecurityResourceDeleted?.Invoke(this, new SecurityAuditDataEventArgs(retVal));
            return retVal;
        }
    }
}
