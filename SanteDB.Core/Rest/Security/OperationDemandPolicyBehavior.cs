using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core.Security.Attribute;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Rest.Security
{
    /// <summary>
    /// Represents a policy behavior for demanding permission
    /// </summary>
    public class OperationDemandPolicyBehavior : IOperationPolicy, IOperationBehavior, IEndpointBehavior
    {

        // The behavior
        private Type m_behaviorType = null;

        /// <summary>
        /// Creates a new demand policy 
        /// </summary>
        public OperationDemandPolicyBehavior(Type behaviorType)
        {
            this.m_behaviorType = behaviorType;
        }
        /// <summary>
        /// Apply the demand
        /// </summary>
        public void Apply(EndpointOperation operation, RestRequestMessage request)
        {
            var methInfo = this.m_behaviorType.GetMethod(operation.Description.InvokeMethod.Name, operation.Description.InvokeMethod.GetParameters().Select(p => p.ParameterType).ToArray());
            foreach (var demand in methInfo.GetCustomAttributes<DemandAttribute>())
                new PolicyPermission(System.Security.Permissions.PermissionState.Unrestricted, demand.PolicyId).Demand();
            
        }

        /// <summary>
        /// Apply the endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            foreach (var op in endpoint.Description.Contract.Operations)
                op.AddOperationBehavior(this);
        }

        /// <summary>
        /// Apply the operation behavior
        /// </summary>
        public void ApplyOperationBehavior(EndpointOperation operation, OperationDispatcher dispatcher)
        {
            dispatcher.AddOperationPolicy(this);
        }
    }
}
