/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.DataTypes
{
    /// <summary>
    /// Represents a human name
    /// </summary>
    [XmlType("HumanName", Namespace = "http://hl7.org/fhir")]
    public class FhirHumanName : FhirElement
    {

        /// <summary>
        /// Suffix
        /// </summary>
        public FhirHumanName()
        {
            this.Family = new List<FhirString>();
            this.Given = new List<FhirString>();
            this.Prefix = new List<FhirString>();
            this.Suffix = new List<FhirString>();
        }

        /// <summary>
        /// Get or sets the use
        /// </summary>
        [XmlElement("use")]
        public FhirCode<String> Use { get; set; }

        /// <summary>
        /// Gets or sets the textual representation of the full name
        /// </summary>
        [XmlElement("text")]
        public FhirString Text { get; set; }

        /// <summary>
        /// Gets or sets the family name
        /// </summary>
        [XmlElement("family")]
        public List<FhirString> Family { get; set; }

        /// <summary>
        /// Gets or sets the given name
        /// </summary>
        [XmlElement("given")]
        public List<FhirString> Given { get; set; }

        /// <summary>
        /// Gets or sets the prefix
        /// </summary>
        [XmlElement("prefix")]
        public List<FhirString> Prefix { get; set; }

        /// <summary>
        /// Gets or sets the suffix name
        /// </summary>
        [XmlElement("suffix")]
        public List<FhirString> Suffix { get; set; }

        /// <summary>
        /// Gets or sets the period
        /// </summary>
        [XmlElement("period")]
        public FhirPeriod Period { get; set; }


        /// <summary>
        /// Write Text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("strong");
            foreach (var n in this.Family)
                w.WriteString(n + " ");
            w.WriteEndElement(); //strong

            w.WriteString(",");
            if (this.Prefix.Count > 0)
            {
                w.WriteStartElement("em");
                foreach (var n in this.Prefix)
                    w.WriteString(n + " ");
                w.WriteEndElement(); //strong
            }

            foreach (var n in this.Given)
                w.WriteString(n + " ");

            if (this.Suffix.Count > 0)
            {
                w.WriteStartElement("em");
                foreach (var n in this.Suffix)
                    w.WriteString(n + " ");
                w.WriteEndElement(); //strong
            }
            w.WriteString(String.Format("({0})", this.Use));
        }

        /// <summary>
        /// Represent as a string
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            foreach (var n in this.Family)
                sb.Append(n + " ");

            sb.Append(",");
            if (this.Prefix.Count > 0)
            {
                foreach (var n in this.Prefix)
                    sb.Append(n + " ");
            }

            foreach (var n in this.Given)
                sb.Append(n + " ");

            if (this.Suffix.Count > 0)
            {
                foreach (var n in this.Suffix)
                    sb.Append(n + " ");
            }

            return sb.ToString();
        }
    }
}
