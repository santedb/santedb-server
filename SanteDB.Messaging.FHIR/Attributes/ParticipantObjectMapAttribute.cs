using MARC.HI.EHRS.SVC.Auditing.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Messaging.FHIR.Attributes
{
    /// <summary>
    /// Identifies how a resource maps to a participant object audit
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ParticipantObjectMapAttribute : System.Attribute
    {

        /// <summary>
        /// Gets or sets the Id type
        /// </summary>
        public AuditableObjectIdType IdType { get; set; }

        /// <summary>
        /// Gets or sets the role
        /// </summary>
        public AuditableObjectRole Role { get; set; }

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        public AuditableObjectType Type { get; set; }

        /// <summary>
        /// Gets the OID name
        /// </summary>
        public string OidName { get; set; }
    }
}
