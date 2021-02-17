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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Base for all resources
    /// </summary>
    [XmlType("DomainResource", Namespace = "http://hl7.org/fhir")]
    public abstract class DomainResourceBase : ResourceBase
    {

        /// <summary>
        /// Namespaces used for serialization
        /// </summary>
        protected XmlSerializerNamespaces m_namespaces = new XmlSerializerNamespaces();

        /// <summary>
        /// Resource tags
        /// </summary>
        public DomainResourceBase()
        {
            this.m_namespaces.Add("", "http://hl7.org/fhir");
            this.Contained = new List<FhirContainedResource>();
        }

        // The narrative
        private Narrative m_narrative;

        /// <summary>
        /// A list of contained resources
        /// </summary>
        [XmlElement("contained")]
        public List<FhirContainedResource> Contained { get; set; }


        /// <summary>
        /// Gets or sets the narrative text
        /// </summary>
        [XmlElement("text")]
        public Narrative Text
        {
            get
            {
                if (this.m_narrative == null && !this.SuppressText)
                    this.m_narrative = this.GenerateNarrative();
                return this.m_narrative;
            }
            set
            {
                this.m_narrative = value;
            }
        }


        /// <summary>
        /// Suppress generation of text
        /// </summary>
        [XmlIgnore]
        public bool SuppressText { get; set; }

        /// <summary>
        /// Generate a narrative
        /// </summary>
        protected Narrative GenerateNarrative()
        {
            // Create a new narrative
            Narrative retVal = new Narrative();

            XmlDocument narrativeContext = new XmlDocument();
            retVal.Status = new FhirCode<string>("generated");
            StringWriter writer = new StringWriter();

            using (XmlWriter xw = XmlWriter.Create(writer, new XmlWriterSettings() { ConformanceLevel = ConformanceLevel.Fragment }))
            {
                xw.WriteStartElement("body", NS_XHTML);
                this.WriteText(xw);

                xw.WriteEndElement();
            }

            narrativeContext.LoadXml(writer.ToString());

            retVal.Div = new XmlElement[narrativeContext.DocumentElement.ChildNodes.Count];
            for (int i = 0; i < retVal.Div.Elements.Length; i++)
                retVal.Div.Elements[i] = narrativeContext.DocumentElement.ChildNodes[i] as XmlElement;
            return retVal;
        }

        /// <summary>
        /// Write text fragement
        /// </summary>
        internal override void WriteText(XmlWriter w)
        {
            w.WriteStartElement("p", NS_XHTML);
            w.WriteString(this.GetType().Name + " - No text defined for resource");
            w.WriteEndElement();
        }

        
        /// <summary>
        /// Add a contained resource
        /// </summary>
        public void AddContainedResource(DomainResourceBase resource)
        {
            resource.MakeIdRef();
            this.Contained.Add(new FhirContainedResource() { Item = resource });
        }
    }
}
