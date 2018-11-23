using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.Everest.Connectors;
using System.Xml.Serialization;
using SanteDB.Messaging.FHIR.Resources;
using System.Xml;
using RestSrvr;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Message processing tool
    /// </summary>
    public static class FhirResourceHandlerUtil
    {

      
        // Message processors
        private static List<IFhirResourceHandler> s_messageProcessors = new List<IFhirResourceHandler>();

        /// <summary>
        /// FHIR message processing utility
        /// </summary>
        static FhirResourceHandlerUtil()
        {

            s_messageProcessors.Add(new StructureDefinitionHandler());

        }

        /// <summary>
        /// Register resource handler
        /// </summary>
        public static void RegisterResourceHandler(IFhirResourceHandler handler)
        {
            s_messageProcessors.Add(handler);
        }

        /// <summary>
        /// Register resource handler
        /// </summary>
        public static void UnRegisterResourceHandler(IFhirResourceHandler handler)
        {
            s_messageProcessors.Remove(handler);
        }
      
        /// <summary>
        /// Get the message processor type based on resource name
        /// </summary>
        public static IFhirResourceHandler GetResourceHandler(String resourceName)
        {
            return s_messageProcessors.Find(o => o.ResourceName.ToLower() == resourceName.ToLower());
        }
        
        /// <summary>
        /// Get REST definition
        /// </summary>
        public static IEnumerable<Backbone.ResourceDefinition> GetRestDefinition()
        {
            return s_messageProcessors.Select(o => {
                var resourceDef = o.GetResourceDefinition();
                var structureDef = o.GetStructureDefinition();
                resourceDef.Profile = Reference.CreateResourceReference(structureDef, RestOperationContext.Current.IncomingRequest.Url);
                return resourceDef;
            });
        }

        /// <summary>
        /// Get all resource handlers
        /// </summary>
        public static IEnumerable<IFhirResourceHandler> ResourceHandlers
        {
            get
            {
                return s_messageProcessors;
            }
        }
    }
}
