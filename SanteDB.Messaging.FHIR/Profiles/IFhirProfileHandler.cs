using Hl7.Fhir.Model;
using SanteDB.Core.BusinessRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.FHIR.Profiles
{
    /// <summary>
    /// Represents a simple profile handler which allows third party operations
    /// </summary>
    public interface IFhirProfileHandler<TBaseResource>
    {

        /// <summary>
        /// Gets the defined profile URI
        /// </summary>
        Uri ProfileUri { get; }

        /// <summary>
        /// Gets the structure definition
        /// </summary>
        List<ElementDefinition> Differential { get; }

        /// <summary>
        /// Validate the resource and emit detected issues
        /// </summary>
        /// <param name="resource">The resource instance</param>
        /// <returns>The list of detected issues</returns>
        List<Core.BusinessRules.DetectedIssue> Validate(TBaseResource resource);

    }
}
