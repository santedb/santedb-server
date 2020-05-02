using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Services;
using System;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace SanteDB.Core.TestFramework
{
    /// <summary>
    /// Configuration service that loads from the test file
    /// </summary>
    public class TestConfigurationService  : IConfigurationManager
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Test Configuration Manager";

        /// <summary>
        /// Get the configuration
        /// </summary>
        public SanteDBConfiguration Configuration { get; set; }

        // Configuration
        private System.Configuration.Configuration m_configuration = null;

        /// <summary>
        /// Get configuration service
        /// </summary>
        public TestConfigurationService()
        {
            try
            {
                var asm = TestApplicationContext.TestAssembly;
                var asmRsn = asm.GetManifestResourceNames().FirstOrDefault(o => o.EndsWith("TestConfig.xml"));
                if (String.IsNullOrEmpty(asmRsn))
                {
                    asm = typeof(TestConfigurationService).Assembly;
                    asmRsn = asm.GetManifestResourceNames().FirstOrDefault(o => o.EndsWith("TestConfig.xml"));
                }
                using (var s = asm.GetManifestResourceStream(asmRsn))
                    this.Configuration = SanteDBConfiguration.Load(s);
            }
            catch { }
        }

        /// <summary>
        /// Get the provided section
        /// </summary>
        public T GetSection<T>() where T : IConfigurationSection
        {
            return this.Configuration.GetSection<T>();
        }

        /// <summary>
        /// Get the specified application setting
        /// </summary>
        public string GetAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        /// <summary>
        /// Get connection string
        /// </summary>
        public ConnectionString GetConnectionString(string key)
        {
            // Use configuration setting 
            ConnectionString retVal = null;
            try
            {
                retVal = this.Configuration.GetSection<DataConfigurationSection>()?.ConnectionString.Find(o => o.Name == key);
            }
            catch { }

            if (retVal == null)
            {
                var cs = this.m_configuration.ConnectionStrings.ConnectionStrings[key];
                if (cs != null)
                {
                    return new ConnectionString(cs.ProviderName, cs.ConnectionString);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Set an application setting
        /// </summary>
        public void SetAppSetting(string key, string value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reload configuration from disk
        /// </summary>
        public void Reload()
        {
            throw new NotSupportedException();
        }
    }
}