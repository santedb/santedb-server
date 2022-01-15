/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */

using SanteDB.Core.Configuration;
using SanteDB.Core.Security.Configuration;
using SanteDB.Docker.Core;
using SanteDB.Server.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Core.Docker
{
    /// <summary>
    /// Security docker feature
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SecurityDockerFeature : IDockerFeature
    {
        /// <summary>
        /// Password pattern setting
        /// </summary>
        public const String PasswordPatternSetting = "PWD_REGEX";

        /// <summary>
        /// Session length policy
        /// </summary>
        public const String SessionLengthSetting = "POL_SESSION";

        /// <summary>
        /// Session length policy
        /// </summary>
        public const String LockoutSetting = "POL_LOCK";

        /// <summary>
        /// Key setting
        /// </summary>
        public const String KeySetting = "SIG_";

        /// <summary>
        /// Gets the ID
        /// </summary>
        public string Id => "SEC";

        /// <summary>
        /// Settings
        /// </summary>
        public IEnumerable<string> Settings => new String[] { PasswordPatternSetting };

        /// <summary>
        /// Configure the setting
        /// </summary>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var secSection = configuration.GetSection<SecurityConfigurationSection>();
            if (secSection == null)
            {
                secSection = new SecurityConfigurationSection()
                {
                    PasswordRegex = @"^(?=.*\d){1,}(?=.*[a-z]){1,}(?=.*[A-Z]){1,}(?=.*[^\w\d]){1,}.{6,}$",
                    PepExemptionPolicy = PolicyEnforcementExemptionPolicy.NoExemptions,

                    SecurityPolicy = new List<SecurityPolicyConfiguration>()
                    {
                        new SecurityPolicyConfiguration(SecurityPolicyIdentification.PasswordHistory, true),
                        new SecurityPolicyConfiguration(SecurityPolicyIdentification.MaxInvalidLogins,5),
                        new SecurityPolicyConfiguration(SecurityPolicyIdentification.SessionLength, (PolicyValueTimeSpan)new TimeSpan(1, 0, 0)),
                        new SecurityPolicyConfiguration(SecurityPolicyIdentification.RefreshLength, (PolicyValueTimeSpan)new TimeSpan(1, 30, 0))
                    },
                    Signatures = new List<SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration>()
                    {
                        new SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration()
                        {
                            Algorithm = SanteDB.Core.Security.Configuration.SignatureAlgorithm.HS256,
                            HmacSecret = "@SanteDBDefault$$$409",
                            KeyName = "jwsdefault"
                        },
                        new SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration()
                        {
                            Algorithm = SanteDB.Core.Security.Configuration.SignatureAlgorithm.HS256,
                            HmacSecret = "@SanteDBDefault$$$409",
                            KeyName = "default"
                        }
                    }
                };
                configuration.AddSection(secSection);
            }

            if (settings.TryGetValue(PasswordPatternSetting, out string passwordRegex))
            {
                secSection.PasswordRegex = passwordRegex;
            }

            if (settings.TryGetValue(LockoutSetting, out string lockout))
            {
                if (!Int32.TryParse(lockout, out int lockoutInt))
                {
                    throw new ArgumentException($"{lockout} is not a valid integer");
                }
                secSection.SetPolicy(SecurityPolicyIdentification.MaxInvalidLogins, lockoutInt);
            }

            if (settings.TryGetValue(SessionLengthSetting, out string sessionLength))
            {
                if (!TimeSpan.TryParse(lockout, out TimeSpan sessionLengthTs))
                {
                    throw new ArgumentException($"{lockout} is not a valid integer");
                }
                secSection.SetPolicy(SecurityPolicyIdentification.SessionLength, (PolicyValueTimeSpan)sessionLengthTs);
            }

            foreach (var set in settings)
            {
                // Key setting
                if (set.Key.StartsWith(KeySetting))
                {
                    var keyName = set.Key.Substring(KeySetting.Length);
                    secSection.Signatures.RemoveAll(o => o.KeyName == keyName);

                    var keyConfigData = set.Value.Split(':');
                    if (keyConfigData.Length != 2)
                    {
                        throw new ArgumentException($"Signature key {set.Key} is invalid, format is - (rs256|rs512|hs256):(keyvalue)");
                    }

                    switch (keyConfigData[0].ToLowerInvariant())
                    {
                        case "hs256":
                            secSection.Signatures.Add(new SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration()
                            {
                                Algorithm = SanteDB.Core.Security.Configuration.SignatureAlgorithm.HS256,
                                HmacSecret = keyConfigData[1],
                                KeyName = keyName
                            });
                            break;

                        case "rs256":
                            secSection.Signatures.Add(new SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration()
                            {
                                Algorithm = SanteDB.Core.Security.Configuration.SignatureAlgorithm.RS256,
                                FindType = System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint,
                                StoreLocation = System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine,
                                StoreName = System.Security.Cryptography.X509Certificates.StoreName.My,
                                FindValue = keyConfigData[1],
                                KeyName = keyName
                            });
                            break;

                        case "rs512":
                            secSection.Signatures.Add(new SanteDB.Core.Security.Configuration.SecuritySignatureConfiguration()
                            {
                                Algorithm = SanteDB.Core.Security.Configuration.SignatureAlgorithm.RS512,
                                FindType = System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint,
                                StoreLocation = System.Security.Cryptography.X509Certificates.StoreLocation.LocalMachine,
                                StoreName = System.Security.Cryptography.X509Certificates.StoreName.My,
                                FindValue = keyConfigData[1],
                                KeyName = keyName
                            });
                            break;
                    }
                }
            }
        }
    }
}