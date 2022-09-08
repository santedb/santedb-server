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
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.Atna.Configuration
{
    /// <summary>
    /// FHIR audit dispatcher feature
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AtnaAuditFeature : IFeature
    {

        // Feature configuration
        private AtnaConfigurationSection m_configuration;

        /// <summary>
        /// Gets the configuration 
        /// </summary>
        public object Configuration
        {
            get => this.m_configuration;
            set => this.m_configuration = (AtnaConfigurationSection)value;
        }

        /// <summary>
        /// Configuration type
        /// </summary>
        public Type ConfigurationType => typeof(AtnaConfigurationSection);

        /// <summary>
        /// Description of the audit dispatcher
        /// </summary>
        public string Description => "Enables dispatching of audits to a remote RFC-3881 or NEMA DICOM server";

        /// <summary>
        /// Flags of this feature
        /// </summary>
        public FeatureFlags Flags => FeatureFlags.None;

        /// <summary>
        /// Group of this feature
        /// </summary>
        public string Group => FeatureGroup.Security;

        /// <summary>
        /// Gets the name of this feature
        /// </summary>
        public string Name => "ATNA Audit Dispatch";

        /// <summary>
        /// Create installation tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateInstallTasks()
        {
            yield return new InstallFhirAuditDispatcher(this, this.m_configuration);
        }

        /// <summary>
        /// Create removal tasks
        /// </summary>
        public IEnumerable<IConfigurationTask> CreateUninstallTasks()
        {
            yield return new UninstallFhirAuditDispatcher(this, this.m_configuration);

        }

        /// <summary>
        /// Query the status of this feature
        /// </summary>
        public FeatureInstallState QueryState(SanteDBConfiguration configuration)
        {
            var auditConfiguration = this.m_configuration = configuration.GetSection<AtnaConfigurationSection>();
            if (auditConfiguration == null)
            {
                auditConfiguration= this.m_configuration = new AtnaConfigurationSection();
                configuration.AddSection(auditConfiguration);
            }

            var service = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Any(r => r.Type == typeof(AtnaAuditService));
            return service && auditConfiguration != null ? FeatureInstallState.Installed : auditConfiguration != null || service ? FeatureInstallState.PartiallyInstalled : FeatureInstallState.NotInstalled;

        }
    }

    /// <summary>
    /// Uninstall the FHIR audit 
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class UninstallFhirAuditDispatcher : IConfigurationTask
    {
        private AtnaConfigurationSection m_configuration;

        /// <summary>
        /// Create a new instance of this task
        /// </summary>
        public UninstallFhirAuditDispatcher(IFeature hostFeature, AtnaConfigurationSection configuration)
        {
            this.m_configuration = configuration;
            this.Feature = hostFeature;
        }

        /// <summary>
        /// Gets the description of this feature
        /// </summary>
        public string Description => "Removes the ATNA audit dispatcher from the system. After this task is executed, the SanteDB server will no longer dispatch audit events to the ATNA audit server";

        /// <summary>
        /// Gets the feature to which this task belongs
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Gets the name of this feature
        /// </summary>
        public string Name => "Remove ATNA Audit Dispatch";

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the removal of the feature
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {
            var dispatcherConfiguration = configuration.GetSection<AtnaConfigurationSection>();
            configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.RemoveAll(r => r.Type == typeof(AtnaAuditService));
            this.ProgressChanged?.Invoke(this, new SanteDB.Core.Services.ProgressChangedEventArgs(1.0f, "Complete"));

            return true;
        }

        /// <summary>
        /// Rollback the configuration
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Verify the state
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration) => configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Any(r => r.Type == typeof(AtnaAuditService));
    }

    /// <summary>
    /// Install the FHIR dispatcher configured in this service
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class InstallFhirAuditDispatcher : IConfigurationTask
    {
        private AtnaConfigurationSection m_configuration;

        /// <summary>
        /// Creates a new installation task
        /// </summary>
        public InstallFhirAuditDispatcher(IFeature hostFeature, AtnaConfigurationSection configuration)
        {
            this.Feature = hostFeature;
            this.m_configuration = configuration;
        }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => $"Installs the ATNA audit dispatcher. Once complete the SanteDB server will send audits in RFC-3881 or NEMA DICOM format to {this.m_configuration.AuditTarget}";

        /// <summary>
        /// Gets the host feature
        /// </summary>
        public IFeature Feature { get; }

        /// <summary>
        /// Get the name of this feature
        /// </summary>
        public string Name => "Install ATNA Audit Dispatch";

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Execute the configuration option
        /// </summary>
        public bool Execute(SanteDBConfiguration configuration)
        {

            configuration.RemoveSection<AtnaConfigurationSection>();
            configuration.AddSection(this.m_configuration);

            configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.RemoveAll(r => typeof(IAuditDispatchService).IsAssignableFrom(r.Type));
            configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.Add(new TypeReferenceConfiguration(typeof(AtnaAuditService)));
            return true;
        }

        /// <summary>
        /// Rollback the configuation
        /// </summary>
        public bool Rollback(SanteDBConfiguration configuration)
        {
            configuration.RemoveSection<AtnaConfigurationSection>();
            configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders.RemoveAll(r => r.Type == typeof(AtnaAuditService));
            return true;
        }

        /// <summary>
        /// Verify the state of this object
        /// </summary>
        public bool VerifyState(SanteDBConfiguration configuration) => true;
    }
}
