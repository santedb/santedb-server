/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB
{
    /// <summary>
    /// Reprsents a service installer.
    /// </summary>
    /// <seealso cref="System.Configuration.Install.Installer" />
    [RunInstaller(true)]
    [ExcludeFromCodeCoverage]
    public class ServiceInstaller : System.Configuration.Install.Installer
    {

        /// <summary>
        /// The m service process installer.
        /// </summary>
        private System.ServiceProcess.ServiceProcessInstaller m_serviceProcessInstaller;

        /// <summary>
        /// The m service installer.
        /// </summary>
        private System.ServiceProcess.ServiceInstaller m_serviceInstaller;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceInstaller"/> class.
        /// </summary>
        public ServiceInstaller()
        {
            // This call is required by the Designer.
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        private void InitializeComponent()
        {
            this.m_serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.m_serviceInstaller = new System.ServiceProcess.ServiceInstaller();

            this.m_serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.m_serviceProcessInstaller.Password = null;
            this.m_serviceProcessInstaller.Username = null;
            this.m_serviceInstaller.ServiceName = "SanteDB";
            this.m_serviceInstaller.Description = "Provides the execution environment for SanteDB";
            this.m_serviceInstaller.DelayedAutoStart = true;
            this.m_serviceInstaller.DisplayName = "SanteDB Host Process";
            this.m_serviceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;

            this.Installers.AddRange(
                new System.Configuration.Install.Installer[]
                {
                    this.m_serviceProcessInstaller,
                    this.m_serviceInstaller
                });
        }
    }
}
