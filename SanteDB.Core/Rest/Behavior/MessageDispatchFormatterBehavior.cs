/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using RestSrvr;
using SanteDB.Core.Rest.Serialization;

namespace SanteDB.Core.Rest.Behavior
{
    /// <summary>
    /// Dispatch formatter behavior
    /// </summary>
    public class MessageDispatchFormatterBehavior : IEndpointBehavior, IOperationBehavior
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

            dispatcher.DispatchFormatter = RestMessageDispatchFormatter.CreateFormatter(operation.Description.Contract.Type);
        }
    }
}
