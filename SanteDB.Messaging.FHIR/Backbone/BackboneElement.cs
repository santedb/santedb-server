using SanteDB.Messaging.FHIR.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Identifies a backbone element
    /// </summary>
    [XmlType("BackboneElement", Namespace = "http://hl7.org/fhir")]
    public abstract class BackboneElement : FhirElement
    {
    }
}
