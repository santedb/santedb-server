using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.Resources;
using MARC.Everest.Connectors;
using MARC.HI.EHRS.SVC.Core.Issues;

namespace SanteDB.Messaging.FHIR
{
    /// <summary>
    /// Represents the outcome of a FHIR operation
    /// </summary>
    public class FhirOperationResult
    {

        /// <summary>
        /// Constructor
        /// </summary>
        public FhirOperationResult()
        {
            this.Details = new List<IResultDetail>();
            this.Issues = new List<DetectedIssue>();
            this.Results = new List<DomainResourceBase>();
        }

        /// <summary>
        /// Gets the overall outcome of the operation
        /// </summary>
        public ResultCode Outcome { get; set; }

        /// <summary>
        /// Represents the results of the operation
        /// </summary>
        public List<DomainResourceBase> Results { get; set; }

        /// <summary>
        /// Business violations
        /// </summary>
        public List<DetectedIssue> Issues { get; set; }

        /// <summary>
        /// Gets the list of details
        /// </summary>
        public List<IResultDetail> Details { get; set; }


    }
}
