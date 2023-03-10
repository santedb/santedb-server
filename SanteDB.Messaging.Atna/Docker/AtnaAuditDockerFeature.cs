/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using SanteDB.Core.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Messaging.Atna.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SanteDB.Messaging.Atna.Docker
{
    /// <summary>
    /// Feature for atna audit shipping
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AtnaAuditDockerFeature : IDockerFeature
    {

        public const string TargetSetting = "TARGET";
        public const string ModeSetting = "MODE";
        public const string SiteSetting = "SITE";
        public const string CertificateSetting = "CERT";

        /// <summary>
        /// ATNA Auditing feature
        /// </summary>
        public string Id => "AUDIT_SHIP";

        /// <summary>
        /// Get the settings
        /// </summary>
        public IEnumerable<string> Settings => new String[]
        {
            TargetSetting, ModeSetting, SiteSetting
        };

        /// <summary>
        /// Configure the host for ATNA audit log shipping
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var atnaConfig = configuration.GetSection<AtnaConfigurationSection>();
            if (atnaConfig == null)
            {
                atnaConfig = new AtnaConfigurationSection()
                {
                    AuditTarget = "sdb-audit:11514",
                    EnterpriseSiteId = "SanteDB^^^SanteDB",
                    Format = AtnaApi.Transport.MessageFormatType.DICOM,
                    Transport = AtnaTransportType.Udp
                };
                configuration.AddSection(atnaConfig);
            }

            // Is the ATNA audit service available?
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            if (!serviceConfiguration.Any(s => s.Type == typeof(AtnaAuditService)))
            {
                serviceConfiguration.Add(new TypeReferenceConfiguration(typeof(AtnaAuditService)));
            }

            // Configure the ATNA service?
            if (settings.TryGetValue(TargetSetting, out string target))
            {
                if (!Uri.TryCreate(target, UriKind.Absolute, out Uri targetUri))
                {
                    throw new ArgumentException($"Target {target} is not a valid URI");
                }

                atnaConfig.AuditTarget = $"{targetUri.Host}:{targetUri.Port}";
                if (!Enum.TryParse<AtnaTransportType>(targetUri.Scheme, true, out AtnaTransportType transport))
                {
                    throw new ArgumentException($"Scheme {targetUri.Scheme} not recognized");
                }
                atnaConfig.Transport = transport;
            }

            if (settings.TryGetValue(ModeSetting, out string mode))
            {
                if (!Enum.TryParse<AtnaApi.Transport.MessageFormatType>(mode, true, out AtnaApi.Transport.MessageFormatType format))
                {
                    throw new ArgumentException($"Format {mode} is not understood");
                }
                atnaConfig.Format = format;
            }

            if (settings.TryGetValue(SiteSetting, out string site))
            {
                atnaConfig.EnterpriseSiteId = site;
            }

            if (settings.TryGetValue(CertificateSetting, out string certThumbprint))
            {
                atnaConfig.ClientCertificate = new SanteDB.Core.Security.Configuration.X509ConfigurationElement()
                {
                    FindType = System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint,
                    FindValue = certThumbprint,
                    StoreLocation = System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine,
                    StoreName = System.Security.Cryptography.X509Certificates.StoreName.My
                };
            }

        }
    }
}
