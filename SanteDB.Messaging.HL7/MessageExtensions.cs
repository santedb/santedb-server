using NHapi.Base.Model;
using NHapi.Model.V25.Datatype;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7
{
    /// <summary>
    /// Represents message extensions
    /// </summary>
    public static class MessageExtensions
    {

        /// <summary>
        /// Determine if the CS is empty
        /// </summary>
        public static bool IsEmpty(this CX me)
        {
            return me.IDNumber.IsEmpty() && me.AssigningAuthority.IsEmpty();
        }

        /// <summary>
        /// Determine if the HD is empty
        /// </summary>
        public static bool IsEmpty(this HD me)
        {
            return me.NamespaceID.IsEmpty() && me.UniversalID.IsEmpty();
        }

        /// <summary>
        /// Determie if TS is empty
        /// </summary>
        public static bool IsEmpty(this TS me)
        {
            return me.Time.IsEmpty();
        }

        /// <summary>
        /// Determines if an abstract primitive is empty
        /// </summary>
        public static bool IsEmpty(this AbstractPrimitive me)
        {
            return String.IsNullOrEmpty(me.Value);
        }

        /// <summary>
        /// Determines if the code is empty
        /// </summary>
        public static bool IsEmpty(this CE me)
        {
            return me.Identifier.IsEmpty() && me.AlternateIdentifier.IsEmpty();
        }
    }
}
