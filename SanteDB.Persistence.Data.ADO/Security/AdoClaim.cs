using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Security.Claims;

namespace SanteDB.Persistence.Data.ADO.Security
{
    /// <summary>
    /// Represents an ADO claim to fulfill the abstracted IClaim interface for PCL libraries
    /// </summary>
    public class AdoClaim : Claim, IClaim
    {
        /// <summary>
        /// Represents an ADO Claim
        /// </summary>
        public AdoClaim(String type, String value) : base(type, value)
        {

        }
        
    }
}
