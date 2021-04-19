using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using System;
using System.Collections.Generic;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data
{
    /// <summary>
    /// Data context extensions
    /// </summary>
    internal static class DataContextExtensions
    {

        /// <summary>
        /// Establish a provenance entry for the specified connection
        /// </summary>
        public static Guid EstablishProvenance(this DataContext me, IPrincipal principal, Guid? externalRef)
        {
            // First, we want to get the identities
            DbSecurityProvenance retVal = new DbSecurityProvenance()
            {
                Key = me.ContextId,
                ExternalSecurityObjectRefKey = externalRef,
                ExternalSecurityObjectRefType = externalRef != null ?
                    (me.Count<DbSecurityUser>(o => o.Key == externalRef) > 0 ? "U" : "P") : null
            };

            // Establish identities
            if (principal is IClaimsPrincipal cprincipal) // claims principal? 
            {
                foreach (var ident in cprincipal.Identities)
                {
                    Guid sid = Guid.Empty;
                    if (ident is AdoIdentity adoIdentity)
                    {
                        sid = adoIdentity.Sid;
                    }
                    else if (ident is IClaimsIdentity cIdentity)
                    {
                        sid = Guid.Parse(cIdentity.FindFirst(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim)?.Value ??
                            cIdentity.FindFirst(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim)?.Value ??
                            cIdentity.FindFirst(SanteDBClaimTypes.Sid)?.Value);
                    }
                    else
                    {
                        throw new SecurityException(ErrorMessages.ERR_SEC_PROVENANCE_UNK_ID);
                    }

                    // Set apporopriate property
                    if (ident is IDeviceIdentity)
                        retVal.DeviceKey = sid;
                    else if (ident is IApplicationIdentity)
                        retVal.ApplicationKey = sid;
                    else
                        retVal.UserKey = sid;
                }

                // Session identifier 
                var sidClaim = cprincipal?.FindFirst(SanteDBClaimTypes.SanteDBSessionIdClaim)?.Value;
                if (Guid.TryParse(sidClaim, out Guid sessionId))
                    retVal.SessionKey = sessionId;

            }
            else // Establish the slow way - using identity name
            {
                if (principal.Identity.Name == AuthenticationContext.SystemPrincipal.Identity.Name)
                    retVal.UserKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                else if (principal.Identity.Name == AuthenticationContext.AnonymousPrincipal.Identity.Name)
                    retVal.UserKey = Guid.Parse(AuthenticationContext.AnonymousUserSid);
                else
                    retVal.UserKey = me.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == principal.Identity.Name.ToLowerInvariant())?.Key;

                if(!retVal.UserKey.HasValue)
                {
                    throw new SecurityException(ErrorMessages.ERR_SEC_PROVENANCE_UNK_ID);
                }
            }

            // insert the provenance object
            try
            {
                if (retVal.UserKey.ToString() == AuthenticationContext.SystemUserSid ||
                    retVal.UserKey.ToString() == AuthenticationContext.AnonymousUserSid)
                {
                    retVal.Key = me.ContextId = retVal.UserKey.Value;
                }
                else
                {
                    retVal = me.Insert(retVal);
                }

                return retVal.Key;

            }
            catch (Exception e)
            {
                throw new SecurityException(ErrorMessages.ERR_SEC_PROVENANCE_GEN_ERR, e);
            }
        }
    }
}
