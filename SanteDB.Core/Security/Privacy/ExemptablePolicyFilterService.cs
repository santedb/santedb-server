﻿/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2020-1-12
 */
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Privacy;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Principal;

namespace SanteDB.Server.Core.Security.Privacy
{
    /// <summary>
    /// Local policy enforcement point service
    /// </summary>
    public class ExemptablePolicyFilterService : DataPolicyFilterService
    {
        // Security configuration
        private SecurityConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        /// <summary>
        /// Creates a new instance with DI
        /// </summary>
        public ExemptablePolicyFilterService(IConfigurationManager configManager, IPasswordHashingService passwordService, IPolicyDecisionService pdpService, IThreadPoolService threadPoolService, IDataCachingService dataCachingService, IAdhocCacheService adhocCache = null, ISubscriptionExecutor subscriptionExecutor = null)
            : base(configManager, passwordService, pdpService, threadPoolService, dataCachingService, subscriptionExecutor, adhocCache)
        {
        }

        /// <summary>
        /// Handle post query event
        /// </summary>
        public override IEnumerable<TData> Apply<TData>(IEnumerable<TData> results, IPrincipal principal)
        {

            // If the current authentication context is a device (not a user) then we should allow the data to flow to the device
            switch(this.m_configuration.PepExemptionPolicy)
            {
                case PolicyEnforcementExemptionPolicy.AllExempt:
                    return results;
                case PolicyEnforcementExemptionPolicy.DevicePrincipalsExempt:
                    if (principal.Identity is DeviceIdentity || principal.Identity is ApplicationIdentity)
                        return results;
                    break;
                case PolicyEnforcementExemptionPolicy.UserPrincipalsExempt:
                    if (!(principal.Identity is DeviceIdentity || principal.Identity is ApplicationIdentity))
                        return results;
                    break;
            }
            return base.Apply(results, principal);
        }


        /// <summary>
        /// Handle post query event
        /// </summary>
        /// 
        public override TData Apply<TData>(TData result, IPrincipal principal)
        {
            if (result == null) // no result
                return null;

            // If the current authentication context is a device (not a user) then we should allow the data to flow to the device
            switch (this.m_configuration.PepExemptionPolicy)
            {
                case PolicyEnforcementExemptionPolicy.AllExempt:
                    return result;
                case PolicyEnforcementExemptionPolicy.DevicePrincipalsExempt:
                    if (principal.Identity is DeviceIdentity || principal.Identity is ApplicationIdentity)
                        return result;
                    break;
                case PolicyEnforcementExemptionPolicy.UserPrincipalsExempt:
                    if (!(principal.Identity is DeviceIdentity || principal.Identity is ApplicationIdentity))
                        return result;
                    break;
            }
            return base.Apply(result, principal);
        }
        

    }
}
