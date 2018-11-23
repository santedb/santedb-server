using MARC.HI.EHRS.SVC.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Messaging.FHIR.Configuration
{
    /// <summary>
    /// FHIR service configuration
    /// </summary>
    public class FhirServiceConfiguration
    {

        /// <summary>
        /// Creates a new instance of the WcfEndpoint
        /// </summary>
        public FhirServiceConfiguration(string wcfEndpoint, string landingPage, Uri baseUri)
        {
            this.WcfEndpoint = wcfEndpoint;
            this.LandingPage = landingPage;
            this.ResourceHandlers = new List<Type>();
            this.ActionMap = new Dictionary<string, CodeValue>();
            this.CorsConfiguration = new Dictionary<string, FhirCorsConfiguration>();
            this.ResourceBaseUri = baseUri;
        }

        /// <summary>
        /// Gets the WCF endpoint name that the FHIR service listens on
        /// </summary>
        public string WcfEndpoint { get; private set; }

        /// <summary>
        /// The landing page file
        /// </summary>
        public string LandingPage { get; private set; }

        /// <summary>
        /// Gets the resource handlers registered
        /// </summary>
        public List<Type> ResourceHandlers { get; private set; }

        /// <summary>
        /// Get or set
        /// </summary>
        public Dictionary<String, CodeValue> ActionMap { get; private set; }

        /// <summary>
        /// Gets the CORS configuration
        /// </summary>
        public Dictionary<String, FhirCorsConfiguration> CorsConfiguration { get;  private set; }

        /// <summary>
        /// When set, describes the base uri for all resources on this FHIR service.
        /// </summary>
        public Uri ResourceBaseUri { get; private set; }
    }

    /// <summary>
    /// FHIR CORS configuration
    /// </summary>
    public class FhirCorsConfiguration
    {

        /// <summary>
        /// Gets or sets the domain from which CORS is allowed
        /// </summary>
        public String Domain { get; set; }

        /// <summary>
        /// Gets or sets the allowed operations
        /// </summary>
        public List<String> Actions { get; set; }

        /// <summary>
        /// Gets or sets the allowed headers
        /// </summary>
        public List<String> Headers { get; set; }
    }
}
