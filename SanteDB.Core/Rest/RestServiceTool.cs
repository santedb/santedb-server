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
