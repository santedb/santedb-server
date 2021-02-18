using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hl7.Fhir.Model.Primitives;
using Hl7.Fhir.Model;
using SanteDB.Core.Model.Interfaces;

namespace SanteDB.Messaging.FHIR.Extensions
{
    /// <summary>
    /// FHIR Extension Handler 
    /// </summary>
    public interface IFhirExtensionHandler
    {

        /// <summary>
        /// Gets the URI of the extension
        /// </summary>
        Uri Uri { get; }
        
        /// <summary>
        /// Gets the resource type that this applies to
        /// </summary>
        ResourceType? AppliesTo { get; }

        /// <summary>
        /// Before returning the model object to the caller
        /// </summary>
        /// <param name="modelObject">The object which the construction occurs from</param>
        /// <returns>The constructed FHIR extension</returns>
        Extension Construct(IIdentifiedEntity modelObject);

        /// <summary>
        /// Parse the specified extension
        /// </summary>
        /// <param name="fhirExtension">The FHIR Extension to parsed</param>
        /// <param name="modelObject">The current model object into which the extension data should be added</param>
        bool Parse(Extension fhirExtension, IdentifiedData modelObject);

    }
}
