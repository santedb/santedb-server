using SanteDB.Core.Security.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Security.Claims
{
    /// <summary>
    /// Claim type handler
    /// </summary>
    public class OverrideClaimHandler : IClaimTypeHandler
    {
        /// <summary>
        /// Claim type handler for override
        /// </summary>
        public string ClaimType => SanteDBClaimTypes.SanteDBOverrideClaim;

        /// <summary>
        /// User wants to override their claims, is this allowed?
        /// </summary>
        public bool Validate(IPrincipal principal, string value)
        {
            Boolean b;
            return Boolean.TryParse(value, out b);
        }
    }
}
