using SanteDB.Core;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Model.Security;
using SanteDB.Persistence.Data.Security;
using SanteDB.Persistence.Data.Services.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace SanteDB.Persistence.Data
{
    /// <summary>
    /// Key harmonization mode
    /// </summary>
    internal enum KeyHarmonizationMode
    {
        /// <summary>
        /// When the harmonization process occurs, the priority is:
        /// Key, if set is used and property is cleared
        /// Property, if set overrides Key
        /// </summary>
        KeyOverridesProperty,

        /// <summary>
        /// When the harmonization process occurs, the priority is:
        /// Property, if set overrides Key
        /// </summary>
        PropertyOverridesKey
    }

    /// <summary>
    /// Data context extensions
    /// </summary>
    internal static class DataContextExtensions
    {
        /// <summary>
        /// Convert validation enforcement to priority
        /// </summary>
        internal static DetectedIssuePriorityType ToPriority(this AdoValidationEnforcement me)
        {
            switch (me)
            {
                case AdoValidationEnforcement.Off:
                    return DetectedIssuePriorityType.Information;

                case AdoValidationEnforcement.Loose:
                    return DetectedIssuePriorityType.Warning;

                case AdoValidationEnforcement.Strict:
                    return DetectedIssuePriorityType.Error;

                default:
                    return DetectedIssuePriorityType.Error;
            }
        }

        /// <summary>
        /// Harmonize the keys with the delay load properties
        /// </summary>
        internal static TData HarmonizeKeys<TData>(this TData me, KeyHarmonizationMode harmonizationMode)
            where TData : IdentifiedData
        {
            me = me.Clone() as TData;

            foreach (var pi in typeof(TData).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (!pi.CanWrite)
                {
                    continue;
                }

                var piValue = pi.GetValue(me);

                // Is the property a key?
                if (piValue is IdentifiedData iddata && iddata.Key.HasValue)
                {
                    // Get the object which references this
                    var keyProperty = pi.GetSerializationRedirectProperty();
                    var keyValue = keyProperty?.GetValue(me);
                    switch (harmonizationMode)
                    {
                        case KeyHarmonizationMode.KeyOverridesProperty:
                            if (keyValue != null) // There is a key for this which is populated, we want to use the key and clear the property
                            {
                                pi.SetValue(me, null);
                            }
                            else
                            {
                                keyProperty.SetValue(me, iddata.Key);
                            }
                            break;

                        case KeyHarmonizationMode.PropertyOverridesKey:
                            if (iddata.Key.HasValue)
                            {
                                keyProperty.SetValue(me, iddata.Key);
                            }
                            else
                            {
                                pi.SetValue(me, null); // Let the identifier data stand
                            }
                            break;
                    }
                }
                else if (piValue is IList list)
                {
                    foreach (var itm in list.OfType<IdentifiedData>())
                    {
                        itm.HarmonizeKeys(harmonizationMode);
                    }
                }
            }
            return me;
        }

        /// <summary>
        /// Get provenance from the context
        /// </summary>
        public static DbSecurityProvenance GetProvenance(this DataContext me)
        {
            if (me.Data.TryGetValue("provenance", out object provenance))
            {
                return provenance as DbSecurityProvenance;
            }
            else
            {
                var retVal = me.FirstOrDefault<DbSecurityProvenance>(o => o.Key == me.ContextId);
                me.AddData("provenance", retVal);
                return retVal;
            }
        }

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
                if (!String.IsNullOrEmpty(sidClaim) && Guid.TryParse(sidClaim, out Guid sessionId))
                    retVal.SessionKey = sessionId;

                // Pure application credential
                if (!retVal.UserKey.HasValue && !retVal.DeviceKey.HasValue)
                {
                    retVal.UserKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                }
                if (retVal.ApplicationKey == Guid.Empty)
                {
                    retVal.ApplicationKey = Guid.Parse(AuthenticationContext.SystemApplicationSid); // System application SID fallback
                }
            }
            else // Establish the slow way - using identity name
            {
                if (principal.Identity.Name == AuthenticationContext.SystemPrincipal.Identity.Name)
                    retVal.UserKey = Guid.Parse(AuthenticationContext.SystemUserSid);
                else if (principal.Identity.Name == AuthenticationContext.AnonymousPrincipal.Identity.Name)
                    retVal.UserKey = Guid.Parse(AuthenticationContext.AnonymousUserSid);
                else
                    retVal.UserKey = me.FirstOrDefault<DbSecurityUser>(o => o.UserName.ToLowerInvariant() == principal.Identity.Name.ToLowerInvariant())?.Key;

                if (!retVal.UserKey.HasValue)
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
                    me.ContextId = retVal.Key;
                }

                me.AddData("provenance", retVal);
                return retVal.Key;
            }
            catch (Exception e)
            {
                throw new SecurityException(ErrorMessages.ERR_SEC_PROVENANCE_GEN_ERR, e);
            }
        }
    }
}