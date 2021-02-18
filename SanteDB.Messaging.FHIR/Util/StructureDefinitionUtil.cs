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
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Model;
using RestSrvr;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Util
{
    /// <summary>
    /// Structure definition utility
    /// </summary>
    public static class StructureDefinitionUtil
    {
        /// <summary>
        /// Get structure definition
        /// </summary>
        public static StructureDefinition GetStructureDefinition(this Type me, bool isProfile = false)
        {

            // Base structure definition
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            var fhirType = me.GetCustomAttribute<FhirTypeAttribute>();

            // Create the structure definition
            var retVal = new StructureDefinition()
            {
                Abstract = me.IsAbstract,
                Contact = new List<ContactDetail>()
                {
                    new ContactDetail()
                    {
                        Name =  me.Assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company
                    }
                },
                Name = me.Name,
                Description = new Markdown(me.GetCustomAttribute<DescriptionAttribute>()?.Description ?? me.Name),
                FhirVersion = FHIRVersion.N4_0_0,
                DateElement = new FhirDateTime(DateTime.Now),
                Kind = fhirType.IsResource ? StructureDefinition.StructureDefinitionKind.Resource : StructureDefinition.StructureDefinitionKind.ComplexType,
                Type = fhirType.Name,
                Derivation = StructureDefinition.TypeDerivationRule.Constraint,
                Id = me.GetCustomAttribute<XmlTypeAttribute>()?.TypeName ?? me.Name,
                Version = entryAssembly.GetName().Version.ToString(),
                VersionId = me.Assembly.GetName().Version.ToString(),
                Copyright = new Markdown(me.Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright),
                Experimental = true,
                Publisher = me.Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company,
                Status = PublicationStatus.Active
            };

            // TODO: Scan for profile handlers
            return retVal;
        }
    }
}
