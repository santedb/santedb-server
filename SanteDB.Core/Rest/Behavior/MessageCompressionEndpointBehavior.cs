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
    /// Adds message compression insepectors
    /// </summary>
    public class MessageCompressionEndpointBehavior : IEndpointBehavior
    {
        /// <summary>
        /// Apply endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            dispatcher.MessageInspectors.Add(new RestCompressionMessageInspector());
        }
    }
}
