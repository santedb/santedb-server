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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.DataTypes;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Backbone
{
    /// <summary>
    /// Software definition
    /// </summary>
    [XmlType("Software", Namespace = "http://hl7.org/fhir")]
    public class SoftwareDefinition : BackboneElement
    {

        /// <summary>
        /// From assembly information
        /// </summary>
        public static SoftwareDefinition FromAssemblyInfo()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            var retVal = new SoftwareDefinition();
            
            // Get assembly attributes
            var title = entryAssembly.GetCustomAttribute<AssemblyTitleAttribute>();
            var version = entryAssembly.GetName().Version.ToString();
            var releaseDate = new FileInfo(Assembly.GetEntryAssembly().Location).LastWriteTime;

            if (title != null)
                retVal.Name = title.Title;
            else
                retVal.Name = entryAssembly.FullName;

            retVal.Version = version;
            retVal.ReleaseDate = releaseDate;

            return retVal;
        }

        /// <summary>
        /// Name of the software
        /// </summary>
        [XmlElement("name")]
        [Description("Name the software is known by")]
        [FhirElement(MinOccurs = 1)]
        public FhirString Name { get; set; }

        /// <summary>
        /// Version of the software
        /// </summary>
        [XmlElement("version")]
        [Description("Version covered by this statement")]
        public FhirString Version { get; set; }

        /// <summary>
        /// Release date of the software
        /// </summary>
        [XmlElement("releaseDate")]
        [Description("The date this version was released")]
        public FhirDateTime ReleaseDate { get; set; }

    }
}
