using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using System;
namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local security application repository
    /// </summary>
    public class LocalSecurityApplicationRepository : GenericLocalSecurityRepository<SecurityApplication>
    {
        protected override string WritePolicy => PermissionPolicyIdentifiers.CreateApplication;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.CreateApplication;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.CreateApplication;

        /// <summary>
        /// Insert the device
        /// </summary>
        public override SecurityApplication Insert(SecurityApplication data)
        {
            if (!String.IsNullOrEmpty(data.ApplicationSecret))
                data.ApplicationSecret = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(data.ApplicationSecret);
            return base.Insert(data);
        }

        /// <summary>
        /// Save the security device
        /// </summary>
        public override SecurityApplication Save(SecurityApplication data)
        {
            if (!String.IsNullOrEmpty(data.ApplicationSecret))
                data.ApplicationSecret = ApplicationServiceContext.Current.GetService<IPasswordHashingService>().ComputeHash(data.ApplicationSecret);
            return base.Save(data);
        }
    }
}
