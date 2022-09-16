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
using SanteDB.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configuration.Tasks
{
    /// <summary>
    /// Represents a task that restarts the SanteDB configured service
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RestartServiceTask : IConfigurationTask
    {
        // Feature reference
        private WindowsServiceFeature m_feature;

        // Configuration references
        private WindowsServiceFeature.Options m_configuration;

        /// <summary>
        /// Restart the service task
        /// </summary>
        public RestartServiceTask()
        {
            this.m_feature = ConfigurationContext.Current.Features.OfType<WindowsServiceFeature>().First();
            this.m_configuration = this.m_feature.Configuration as WindowsServiceFeature.Options;
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        public string Name => $"Restart {this.m_configuration.ServiceName}";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Restarts the {this.m_configuration.ServiceName} windows service";

        /// <summary>
        /// Gets the feature
        /// </summary>
        public IFeature Feature => this.m_feature;

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the operation
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            ServiceTools.ServiceInstaller.StopService(this.m_configuration.ServiceName);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (sw.ElapsedMilliseconds < 10000 && ServiceTools.ServiceInstaller.GetServiceStatus(this.m_configuration.ServiceName) != ServiceTools.ServiceState.Stop)
            {
                Application.DoEvents();
                Thread.Sleep(1000);
                this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((float)sw.ElapsedMilliseconds / 10000f, $"Stopping {this.m_configuration.ServiceName}"));
            } // HACK: wait for stop
            this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(0.5f, $"Starting {this.m_configuration.ServiceName}"));
            ServiceTools.ServiceInstaller.StartService(this.m_configuration.ServiceName);
            while (sw.ElapsedMilliseconds < 20000 && ServiceTools.ServiceInstaller.GetServiceStatus(this.m_configuration.ServiceName) == ServiceTools.ServiceState.Starting)
            {
                Application.DoEvents();
                Thread.Sleep(1000);
                this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(0.5f + ((float)sw.ElapsedMilliseconds / 20000f), $"Starting {this.m_configuration.ServiceName}"));
            } // HACK: wait for stop
            this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(1.0f, null));
            return true;
        }

        /// <summary>
        /// Rollback the changes
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            return false;
        }

        /// <summary>
        /// Verify state
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration)
        {
            return ServiceTools.ServiceInstaller.ServiceIsInstalled(this.m_configuration.ServiceName);
        }
    }
}