using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Collections.Specialized;

namespace SanteDB.Messaging.FHIR.Handlers
{

    /// <summary>
    /// Represents a class that can handle a FHIR resource query request
    /// </summary>
    public interface IFhirResourceHandler
    {

        /// <summary>
        /// Gets the type of resource this handler can perform operations on
        /// </summary>
        string ResourceName { get; }

        /// <summary>
        /// Read a specific version of a resource
        /// </summary>
        FhirOperationResult Read(string id, string versionId);

        /// <summary>
        /// Update a resource
        /// </summary>
        FhirOperationResult Update(string id, DomainResourceBase target, TransactionMode mode);

        /// <summary>
        /// Delete a resource
        /// </summary>
        FhirOperationResult Delete(string id, TransactionMode mode);

        /// <summary>
        /// Create a resource
        /// </summary>
        FhirOperationResult Create(DomainResourceBase target, TransactionMode mode);

        /// <summary>
        /// Query a FHIR resource
        /// </summary>
        FhirQueryResult Query(NameValueCollection parameters);

        /// <summary>
        /// Get the definition for this resource
        /// </summary>
        Backbone.ResourceDefinition GetResourceDefinition();

        /// <summary>
        /// Get the structure definition for this profile
        /// </summary>
        StructureDefinition GetStructureDefinition();
    }
}
