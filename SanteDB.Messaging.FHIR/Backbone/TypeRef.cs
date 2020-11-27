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
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Type reference of the profile
    /// </summary>
    [XmlType("TypeRef", Namespace = "http://hl7.org/fhir")]
    public class TypeRef : BackboneElement
    {

        private Type m_type = null;

        /// <summary>
        /// Creates a new typeref
        /// </summary>
        public TypeRef()
        {
        }

        /// <summary>
        /// Creates a new type ref from the .NET type
        /// </summary>
        public static TypeRef MakeTypeRef(Type type)
        {
            
            var retVal = new TypeRef();
            
            // Get the FHIR name
            Type realType = type;
            while(realType.IsGenericType && !realType.Namespace.EndsWith("DataTypes"))
                realType = realType.GetGenericArguments()[0];
            XmlTypeAttribute xta = realType.GetCustomAttribute<XmlTypeAttribute>();
            retVal.m_type = realType;
            if (xta == null)
                return null;
            else if (typeof(DomainResourceBase).IsAssignableFrom(realType))
                retVal.Code = new FhirCode<string>(String.Format("Resource({0})", xta.TypeName));
            else
                retVal.Code = new FhirCode<string>(xta.TypeName);

            return retVal;
        }

        /// <summary>
        /// The codified primitive
        /// </summary>
        [XmlElement("code")]
        public FhirCode<String> Code { get; set; }

        /// <summary>
        /// The url to the code profile
        /// </summary>
        [XmlElement("profile")]
        public FhirUri Uri { get; set; }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {


            if (this.m_type != null)
            {

                // First a reasource?
                if (typeof(DomainResourceBase).IsAssignableFrom(this.m_type))
                {
                    w.WriteString("Resource(");
                    var tn = this.m_type.GetCustomAttribute<XmlTypeAttribute>().TypeName;
                    w.WriteStartElement("a");
                    w.WriteAttributeString("href", String.Format("http://hl7.org/implement/standards/fhir/{0}.htm", tn));
                    w.WriteString(tn);
                    w.WriteEndElement(); // a
                    w.WriteString(") ");
                }
                else if (this.m_type.Namespace.EndsWith("DataTypes"))
                {
                    var tn = this.m_type.GetCustomAttribute<XmlTypeAttribute>().TypeName;
                    w.WriteStartElement("a");
                    w.WriteAttributeString("href", String.Format("http://hl7.org/implement/standards/fhir/datatypes.htm#{0}", this.Code.Value));
                    w.WriteString(tn);
                    w.WriteEndElement(); // a
                }

            }
        }
    }
}
