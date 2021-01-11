using SanteDB.Core.Model;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.FHIR.Extensions
{
    /// <summary>
    /// FHIR Extension Handler 
    /// </summary>
    public interface IFhirExtensionHandler<TExtendedResource>
        where TExtendedResource : ResourceBase
    {

        /// <summary>
        /// Gets the URI of the extension
        /// </summary>
        Uri Uri { get; }

        /// <summary>
        /// Before returning the model object to the caller
        /// </summary>
        /// <param name="modelObject">The object which the construction occurs from</param>
        /// <returns>The constructed FHIR extension</returns>
        Extension Construct(IdentifiedData modelObject);

        /// <summary>
        /// Parse the specified extension
        /// </summary>
        /// <param name="fhirExtension">The FHIR Extension to parsed</param>
        /// <param name="modelObject">The current model object into which the extension data should be added</param>
        void Parse(Extension fhirExtension, IdentifiedData modelObject);

    }
}
