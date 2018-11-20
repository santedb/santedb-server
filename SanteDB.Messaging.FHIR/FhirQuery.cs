using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace SanteDB.Messaging.FHIR
{
    /// <summary>
    /// Internal query structure
    /// </summary>
    public class FhirQuery
    {
        /// <summary>
        /// FHIR query
        /// </summary>
        public FhirQuery()
        {
            this.ActualParameters = new NameValueCollection();
            this.QueryId = Guid.Empty;
            this.IncludeHistory = false;
            this.MinimumDegreeMatch = 1.0f;
            this.TargetDomains = new List<String>();
            this.Start = 0;
            this.Quantity = 25;
        }

        /// <summary>
        /// Get the actual parameters that could be serviced
        /// </summary>
        public NameValueCollection ActualParameters { get; set; }

        /// <summary>
        /// Identifies the query identifier
        /// </summary>
        public Guid QueryId { get; set; }

        /// <summary>
        /// True if the query is merely a sumary
        /// </summary>
        public bool IncludeHistory { get; set; }

        /// <summary>
        /// True if the query should include contained resource
        /// </summary>
        public bool IncludeContained { get; set; }

        /// <summary>
        /// Include resources
        /// </summary>
        public List<String> IncludeResource { get; set; }

        /// <summary>
        /// Gets or sets the target domains
        /// </summary>
        public List<String> TargetDomains { get; set; }

        /// <summary>
        /// Minimum degree natcg
        /// </summary>
        public float MinimumDegreeMatch { get; set; }

        /// <summary>
        /// Start result
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// The Quantity
        /// </summary>
        public int Quantity { get; set; }

    }
}
