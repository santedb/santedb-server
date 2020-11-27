/*
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
 * Date: 2019-11-27
 */
using SanteDB.Core;
using SanteDB.Core.Data;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.ADO.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.TestFramework
{
    /// <summary>
    /// Represents the test context
    /// </summary>
    public class TestApplicationContext : ApplicationContext
    {

        /// <summary>
        /// Gets the host type
        /// </summary>
        public override SanteDBHostType HostType => SanteDBHostType.Test;

        /// <summary>
        /// Gets or set sthe test assembly
        /// </summary>
        public static Assembly TestAssembly { get; set; }

        /// <summary>
        /// Creastes a new test context
        /// </summary>
        public TestApplicationContext()
        {
            this.ContextId = Guid.NewGuid();
            this.RemoveServiceProvider(typeof(IConfigurationManager));
            this.AddServiceProvider(typeof(TestConfigurationService));
        }

        /// <summary>
        /// Initialize the test context
        /// </summary>
        /// <param name="deploymentDirectory"></param>
        public static void Initialize(String deploymentDirectory)
        {

            if (ApplicationServiceContext.Current != null) return;

            AppDomain.CurrentDomain.SetData(
               "DataDirectory",
               Path.Combine(deploymentDirectory, string.Empty));

            EntitySource.Current = new EntitySource(new PersistenceEntitySource());
            ApplicationServiceContext.Current = ApplicationContext.Current = new TestApplicationContext();
            ApplicationContext.Current.Start();

            // Start the daemon services
            var adoPersistenceService = ApplicationServiceContext.Current.GetService<AdoPersistenceService>();
            if (adoPersistenceService?.IsRunning == false)
            {
                //adoPersistenceService.Start();
                TestApplicationContext.Current.Start();
            }
        }
    }
}
