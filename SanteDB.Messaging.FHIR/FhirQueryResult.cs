using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MARC.HI.EHRS.SVC.Core.Issues;
using SanteDB.Messaging.FHIR.Resources;

namespace SanteDB.Messaging.FHIR
{
    /// <summary>
    /// Query result form a FHIR query
    /// </summary>
    public class FhirQueryResult : FhirOperationResult
    {

        /// <summary>
        /// Gets or sets the query that initiated the action
        /// </summary>
        public FhirQuery Query { get; set; }

        /// <summary>
        /// Gets the total results
        /// </summary>
        public int TotalResults { get; set; }

    }
}
