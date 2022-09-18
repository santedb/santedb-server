/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-5-30
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Services;
using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace SanteDB.Core.TestFramework
{
    /// <summary>
    /// Configuration service that loads from the test file
    /// </summary>
    [ExcludeFromCodeCoverage]
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
            return this.Configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings.FirstOrDefault(o=>o.Key == key)?.Value;
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