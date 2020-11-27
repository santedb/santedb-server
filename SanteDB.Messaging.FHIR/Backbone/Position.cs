/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Represents a physical position
    /// </summary>
    [XmlType("Position", Namespace = "http://hl7.org/fhir")]
    public class Position : BackboneElement
    {

        /// <summary>
        /// Gets or sets the latitude
        /// </summary>
        [XmlElement("latitude")]
        [Description("The latitude of the physical position")]
        public FhirDecimal Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude
        /// </summary>
        [XmlElement("longitude")]
        [Description("The longitude of the physical location")]
        public FhirDecimal Longitude { get; set; }

        /// <summary>
        /// Gets or sets the altitude of the position
        /// </summary>
        [XmlElement("altitude")]
        public FhirDecimal Altitude { get; set; }

        /// <summary>
        /// Write the position as text
        /// </summary>
        internal override void WriteText(XmlWriter w)
        {
            w.WriteString(" lat=");
            this.Latitude?.WriteText(w);
            w.WriteString(" lng=");
            this.Longitude?.WriteText(w);
        }
    }
}
