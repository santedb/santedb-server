using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.AMI.Resources
{
    /// <summary>
    /// Provenance
    /// </summary>
    public class SecurityProvenanceHandler : ResourceHandlerBase<SecurityProvenance>
    {
        /// <summary>
        /// Capabilities
        /// </summary>
        public override ResourceCapability Capabilities => ResourceCapability.Get;

        /// <summary>
        /// Get the specified object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public override object Get(object id, object versionId)
        {
            return ApplicationContext.Current.GetService<ISecurityRepositoryService>().GetProvenance((Guid)id);
        }
    }
}
