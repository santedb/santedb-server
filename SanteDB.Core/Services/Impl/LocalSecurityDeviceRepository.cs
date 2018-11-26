using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local security device repository
    /// </summary>
    public class LocalSecurityDeviceRepository : GenericLocalSecurityRepository<SecurityDevice>
    {
        protected override string WritePolicy => PermissionPolicyIdentifiers.CreateDevice;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.CreateDevice;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.CreateDevice;

        /// <summary>
        /// Insert the device
        /// </summary>
        public override SecurityDevice Insert(SecurityDevice data)
        {
            if (!String.IsNullOrEmpty(data.DeviceSecret))
                data.DeviceSecret = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(data.DeviceSecret);
            return base.Insert(data);
        }

        /// <summary>
        /// Save the security device
        /// </summary>
        public override SecurityDevice Save(SecurityDevice data)
        {
            if (!String.IsNullOrEmpty(data.DeviceSecret))
                data.DeviceSecret = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(data.DeviceSecret);
            return base.Save(data);
        }
    }
}
