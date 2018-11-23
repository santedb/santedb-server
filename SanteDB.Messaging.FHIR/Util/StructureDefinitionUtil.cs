using RestSrvr;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
                FhirVersion = "3.0.1",
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
