/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
            var asm = TestApplicationContext.TestAssembly;
            var asmRsn = asm?.GetManifestResourceNames().FirstOrDefault(o => o.EndsWith("TestConfig.xml"));
            if (String.IsNullOrEmpty(asmRsn))
            {
                asm = typeof(TestConfigurationService).Assembly;
                asmRsn = asm.GetManifestResourceNames().FirstOrDefault(o => o.EndsWith("TestConfig.xml"));
            }
            using (var s = asm.GetManifestResourceStream(asmRsn))
                this.Configuration = SanteDBConfiguration.Load(s);

            if (this.Configuration == null)
                throw new InvalidOperationException("Could not load test configuration. Ensure that TestConfig.xml is in your project and set as an EmbeddedResource");
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