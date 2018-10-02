/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: fyfej
 * Date: 2017-9-1
 */
using SanteDB.Core.Model.Roles;
using SanteDB.Messaging.HL7.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Messaging.HL7.ParameterMap
{

    /// <summary>
    /// Represents a query parameter map
    /// </summary>
    [XmlType(nameof(Hl7QueryParameterMap), Namespace = "http://santedb.org/model/hl7")]
    [XmlRoot(nameof(Hl7QueryParameterMap), Namespace = "http://santedb.org/model/hl7")]
    public class Hl7QueryParameterMap
    {

        /// <summary>
        /// The type of the map
        /// </summary>
        [XmlElement("messageType")]
        public List<Hl7QueryParameterType> Map { get; set; }

        /// <summary>
        /// Merges two query parameter maps together
        /// </summary>
        public void Merge(Hl7QueryParameterMap map)
        {

            foreach (var itm in map.Map)
            {
                var myMapping = this.Map.FirstOrDefault(p => p.Trigger == itm.Trigger);

                // I have a local mapping
                if (myMapping != null)
                {
                    // Remove any overridden mappings
                    myMapping.Parameters.RemoveAll(o => itm.Parameters.Any(i => i.Hl7Name == o.Hl7Name));
                    // Add overridden mappings
                    myMapping.Parameters.AddRange(itm.Parameters);
                }
                else // we just add
                    this.Map.Add(itm);

            }
        }
    }

    /// <summary>
    /// Represents a query parameter map
    /// </summary>
    [XmlType(nameof(Hl7QueryParameterType), Namespace = "http://santedb.org/model/hl7")]
    public class Hl7QueryParameterType
    {

        /// <summary>
        /// Trigger for this query
        /// </summary>
        [XmlAttribute("trigger")]
        public String Trigger { get; set; }

        /// <summary>
        /// Response type
        /// </summary>
        [XmlAttribute("response")]
        public String ResponseTrigger { get; set; }

        /// <summary>
        /// Gets or sets the source type
        /// </summary>
        [XmlIgnore]
        public Type QueryTarget { get; set; }

        /// <summary>
        /// The model type
        /// </summary>
        [XmlAttribute("queryTarget")]
        public String QueryTargetXml {
            get { return this.QueryTarget.GetCustomAttribute<XmlRootAttribute>().ElementName; }
            set { this.QueryTarget = new SanteDB.Core.Model.Serialization.ModelSerializationBinder().BindToType(typeof(Patient).Assembly.FullName, value); }
        }

        /// <summary>
        /// Gets or sets the result handler
        /// </summary>
        [XmlIgnore]
        public IQueryResultHandler ResultHandler { get; set; }

        /// <summary>
        /// The name of the fuzzy probabalistic algorithm to score matches
        /// </summary>
        [XmlAttribute("resultHandler")]
        public String ResultHandlerXml
        {
            get { return this.ResultHandler.GetType().AssemblyQualifiedName; }
            set { this.ResultHandler = Activator.CreateInstance(Type.GetType(value)) as IQueryResultHandler; }
        }

        /// <summary>
        /// Gets the match configuration
        /// </summary>
        [XmlAttribute("scoreConfiguration")]
        public String ScoreConfiguration { get; set; }

        /// <summary>
        /// Map the query parameter
        /// </summary>
        [XmlElement("parameter")]
        public List<Hl7QueryParameterMapProperty> Parameters { get; set; }

    }

    /// <summary>
    /// Represents a query parameter map 
    /// </summary>
    [XmlType(nameof(Hl7QueryParameterMapProperty), Namespace = "http://santedb.org/model/hl7")]
    public class Hl7QueryParameterMapProperty
    {

        /// <summary>
        /// The model query parameter
        /// </summary>
        [XmlAttribute("model")]
        public String ModelName { get; set; }

        /// <summary>
        /// The FHIR name
        /// </summary>
        [XmlAttribute("hl7")]
        public String Hl7Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the fhir parmaeter
        /// </summary>
        [XmlAttribute("type")]
        public String ParameterType { get; set; }

        /// <summary>
        /// Gets or sets the textual description of the query parameter
        /// </summary>
        [XmlAttribute("desc")]
        public String Description { get; set; }

        /// <summary>
        /// The function call to set on the rhs of the =
        /// </summary>
        [XmlAttribute("transform")]
        public String ValueTransform { get; set; }

    }

}
