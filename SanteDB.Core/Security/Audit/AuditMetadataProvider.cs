using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSrvr;
using SanteDB.Core.Auditing;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Services;

namespace SanteDB.Core.Security.Audit
{
    /// <summary>
    /// Audit metadata provider
    /// </summary>
    public class AuditMetadataProvider : IAuditMetadataProvider
    {
        /// <summary>
        /// Gets metadata for all audits
        /// </summary>
        public IDictionary<AuditMetadataKey, object> GetMetadata()
        {
            return new Dictionary<AuditMetadataKey, object>()
            {
                { AuditMetadataKey.PID, Process.GetCurrentProcess().Id },
                { AuditMetadataKey.ProcessName, Process.GetCurrentProcess().ProcessName },
                { AuditMetadataKey.SessionId, (AuthenticationContext.Current.Principal as IClaimsPrincipal)?.FindFirst(SanteDBClaimTypes.SanteDBSessionIdClaim)?.Value },
                { AuditMetadataKey.CorrelationToken, RestOperationContext.Current?.Data["uuid"] },
                {AuditMetadataKey.AuditSourceType, "ApplicationServerProcess" },
                { AuditMetadataKey.LocalEndpoint, RestOperationContext.Current?.IncomingRequest.Url },
                { AuditMetadataKey.RemoteHost, ApplicationServiceContext.Current.GetService<IRemoteEndpointResolver>()?.GetRemoteEndpoint() }

            };
        }
    }
}
