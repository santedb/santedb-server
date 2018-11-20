using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Identity reference type
    /// </summary>
    [XmlType("IdRef", Namespace = "http://hl7.org/fhir")]
    public class IdRef 
    {

        /// <summary>
        /// The value of the IDRef
        /// </summary>
        [XmlAttribute("value")]
        public String Value { get; set; }

        /// <summary>
        /// Resolve reference
        /// </summary>
        public FhirElement ResolveReference(FhirElement context)
        {
            return new FhirElement() { IdRef = this.Value }.ResolveReference(context);
        }
    }
}
