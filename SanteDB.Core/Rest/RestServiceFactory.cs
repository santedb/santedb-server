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
using RestSrvr.Attributes;
using RestSrvr.Bindings;
using SanteDB.Core.Configuration;
using SanteDB.Core.Interop;
using SanteDB.Core.Rest.Behavior;
using SanteDB.Core.Rest.Security;
using SanteDB.Core.Services;
using System;
using System.Linq;
using System.Reflection;

namespace SanteDB.Core.Rest
{
    /// <summary>
    /// Rest service tool to create rest services
    /// </summary>
    public class RestServiceFactory : IRestServiceFactory
    {
        /// <summary>
        /// Get capabilities
        /// </summary>
        public int GetServiceCapabilities(RestService me)
        {
            var retVal = ServiceEndpointCapabilities.None;
            // Any of the capabilities are for security?
            if (me.ServiceBehaviors.OfType<TokenAuthorizationAccessBehavior>().Any())
                retVal |= ServiceEndpointCapabilities.BearerAuth;
            if (me.ServiceBehaviors.OfType<BasicAuthorizationAccessBehavior>().Any())
                retVal |= ServiceEndpointCapabilities.BasicAuth;
            if (me.Endpoints.Any(e => e.Behaviors.OfType<MessageCompressionEndpointBehavior>().Any()))
                retVal |= ServiceEndpointCapabilities.Compression;
            if (me.Endpoints.Any(e => e.Behaviors.OfType<CorsEndpointBehavior>().Any()))
                retVal |= ServiceEndpointCapabilities.Cors;
            if (me.Endpoints.Any(e => e.Behaviors.OfType<MessageDispatchFormatterBehavior>().Any()))
                retVal |= ServiceEndpointCapabilities.ViewModel;
            return (int)retVal;

        }

        /// <summary>
        /// Create the rest service
        /// </summary>
        public RestService CreateService(Type serviceType)
        {
            // Get the configuration
            var configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<RestConfigurationSection>();
            var sname = serviceType.GetCustomAttribute<ServiceBehaviorAttribute>()?.Name ?? serviceType.FullName;
            var config = configuration.Services.FirstOrDefault(o => o.Name == sname);
            if (config == null)
                throw new InvalidOperationException($"Cannot find configuration for {sname}");
            var retVal = new RestService(serviceType);
            foreach (var bhvr in config.Behaviors)
                retVal.AddServiceBehavior(
                    bhvr.Configuration == null ?
                    Activator.CreateInstance(bhvr.Type) as IServiceBehavior :
                    Activator.CreateInstance(bhvr.Type, bhvr.Configuration) as IServiceBehavior);

            var demandPolicy = new OperationDemandPolicyBehavior(serviceType);

            foreach (var ep in config.Endpoints)
            {
                var se = retVal.AddServiceEndpoint(new Uri(ep.Address), ep.Contract, new RestHttpBinding());
                foreach (var bhvr in ep.Behaviors)
                {
                    se.AddEndpointBehavior(
                        bhvr.Configuration == null ?
                        Activator.CreateInstance(bhvr.Type) as IEndpointBehavior :
                        Activator.CreateInstance(bhvr.Type, bhvr.Configuration) as IEndpointBehavior);
                    se.AddEndpointBehavior(demandPolicy);
                }
            }
            return retVal;


        }
    }
}
