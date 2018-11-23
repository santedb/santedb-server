using RestSrvr;
using SanteDB.Core.Rest.Serialization;
using SanteDB.Messaging.FHIR.Rest.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.FHIR.Rest.Behavior
{
    /// <summary>
    /// Dispatch formatter behavior
    /// </summary>
    public class FhirMessageDispatchFormatterEndpointBehavior : IEndpointBehavior, IOperationBehavior
    {
        /// <summary>
        /// Apply the behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            foreach (var op in endpoint.Description.Contract.Operations)
                op.AddOperationBehavior(this);
        }

        /// <summary>
        /// Apply operation behavior
        /// </summary>
        public void ApplyOperationBehavior(EndpointOperation operation, OperationDispatcher dispatcher)
        {
            dispatcher.DispatchFormatter = new FhirMessageDispatchFormatter();
        }
    }
}
