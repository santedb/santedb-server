/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-11-23
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Bindings;
using SanteDB.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Rest
{
    /// <summary>
    /// Rest service tool to create rest services
    /// </summary>
    public static class RestServiceTool
    {
        // Master configuration
        private static SanteDBConfiguration s_config = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("santedb.core") as SanteDBConfiguration;

        /// <summary>
        /// Create the rest service
        /// </summary>
        public static RestService CreateService(Type serviceType)
        {
            // Get the configuration
            var sname = serviceType.GetCustomAttribute<ServiceBehaviorAttribute>()?.Name ?? serviceType.FullName;
            var config = s_config.RestConfiguration.Services.FirstOrDefault(o => o.Name == sname);
            if (config == null)
                throw new InvalidOperationException($"Cannot find configuration for {sname}");
            var retVal = new RestService(serviceType);
            foreach (var bhvr in config.Behaviors)
                retVal.AddServiceBehavior(Activator.CreateInstance(bhvr) as IServiceBehavior);
            foreach (var ep in config.Endpoints)
                retVal.AddServiceEndpoint(ep.Address, ep.Contract, new RestHttpBinding());
            return retVal;


        }
    }
}
