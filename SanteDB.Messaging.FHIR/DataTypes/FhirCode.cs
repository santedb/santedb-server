using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Primitive code value
    /// </summary>
    [XmlType(Namespace = "http://hl7.org/fhir")]
    [Serializable]
    public class FhirCode<T> : Primitive<T>
    {

        /// <summary>
        /// Constructs a new instance of code
        /// </summary>
        public FhirCode()
        {

        }

        /// <summary>
        /// Creates a new instance of the code
        /// </summary>
        public FhirCode(T code) : base(code)
        {
        }


        /// <summary>
        /// Create fhir code from prinitive
        /// </summary>
        /// <param name="v"></param>
        public static implicit operator FhirCode<T>(T v)
        {
            return new FhirCode<T>(v);
        }
    }
}
