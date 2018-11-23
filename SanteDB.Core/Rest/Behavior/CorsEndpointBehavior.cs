using RestSrvr;
using SanteDB.Core.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Rest.Behavior
{
    /// <summary>
    /// Adds message CORS insepectors
    /// </summary>
    public class CorsEndpointBehavior : IEndpointBehavior
    {

        // Settings
        private CorsSettings m_settings;

        /// <summary>
        /// Creates a new CORS endpoint behavior
        /// </summary>
        public CorsEndpointBehavior(CorsSettings settings)
        {
            this.m_settings = settings;
        }

        /// <summary>
        /// Apply endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(new CorsMessageInspector(this.m_settings));
        }
    }
}
