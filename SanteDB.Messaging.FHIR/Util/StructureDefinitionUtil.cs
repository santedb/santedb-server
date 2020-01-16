/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using RestSrvr;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
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
            FhirUri baseStructureDefn = null;
            if (isProfile)
                baseStructureDefn = new Uri(Reference.CreateResourceReference<StructureDefinition>(me.GetStructureDefinition(false), RestOperationContext.Current.IncomingRequest.Url).ReferenceUrl, UriKind.Relative);
            else if (typeof(ResourceBase).IsAssignableFrom(me.BaseType))
                baseStructureDefn = new Uri($"http://hl7.org/fhir/StructureDefinition/{me.BaseType.GetCustomAttribute<XmlTypeAttribute>().TypeName}", UriKind.Absolute);

            // Create the structure definition
            var retVal = new StructureDefinition()
            {
                Abstract = me.IsAbstract,
                Base = baseStructureDefn,
                Contact = new List<Backbone.ContactDetail>()
                {
                    new Backbone.ContactDetail()
                    {
                        Name =  me.Assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company
                    }
                },
                Name = me.GetCustomAttribute<DescriptionAttribute>()?.Description ?? me.Name,
                Description = me.GetCustomAttribute<DescriptionAttribute>()?.Description ?? me.Name,
                FhirVersion = "4.0.0",
                Date = DateTime.Now,
                Kind = StructureDefinitionKind.Resource,
                Type = me.GetCustomAttribute<XmlTypeAttribute>()?.TypeName ?? me.Name,
                DerivationType = isProfile ? TypeDerivationRule.Constraint : TypeDerivationRule.Specialization,
                Id = me.GetCustomAttribute<XmlTypeAttribute>()?.TypeName ?? me.Name,
                Version = entryAssembly.GetName().Version.ToString(),
                VersionId = me.Assembly.GetName().Version.ToString(),
                Copyright = me.Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright,
                Experimental = true,
                Publisher = me.Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company,
                Status = PublicationStatus.Active
            };

            // Structure definitions
            return retVal;
        }
    }
}
