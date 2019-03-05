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
using SanteDB.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local service manager
    /// </summary>
    public class LocalServiceManager : IServiceManager
    {
		/// <summary>
		/// Add service provider
		/// </summary>
		/// <param name="serviceType">Type of the service.</param>
		public void AddServiceProvider(Type serviceType)
        {
            ApplicationContext.Current.AddServiceProvider(serviceType);
        }

        /// <summary>
        /// Get all services
        /// </summary>
        public IEnumerable<object> GetServices()
        {
            return ApplicationContext.Current.GetServices();
        }


        /// <summary>
        /// Remove service provider
        /// </summary>
        public void RemoveServiceProvider(Type serviceType)
        {
            ApplicationContext.Current.RemoveServiceProvider(serviceType);
        }

        /// <summary>
        /// Get all types
        /// </summary>
        public IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.IsDynamic)
                .SelectMany(a => a.ExportedTypes);
        }
    }
}
