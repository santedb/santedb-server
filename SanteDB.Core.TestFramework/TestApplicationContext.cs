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
