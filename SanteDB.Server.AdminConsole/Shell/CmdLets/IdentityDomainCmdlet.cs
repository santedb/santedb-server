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
 * Date: 2022-9-7
 */
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Messaging.HDSI.Client;
using SanteDB.Server.AdminConsole.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Interop;
using SanteDB.Server.AdminConsole.Util;

namespace SanteDB.Server.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Administrative commandlet for assigning authorities
    /// </summary>
    [AdminCommandlet]
    [ExcludeFromCodeCoverage]
    public static class IdentityDomainCmdlet
    {
        // Ami client
        private static AmiServiceClient m_amiClient = new AmiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.AdministrationIntegrationService));
        private static HdsiServiceClient m_hdsiClient = new HdsiServiceClient(ApplicationContext.Current.GetRestClient(ServiceEndpointType.HealthDataService));

        static IdentityDomainCmdlet()
        {
            m_hdsiClient.Client.Requesting += (o, e) => e.Query.Add("_lean", "true");
        }
        /// <summary>
        /// Authority params base
        /// </summary>
        public class IdentityDomainParamBase
        {
            /// <summary>
            /// Gets or sets the authority
            /// </summary>
            [Description("The authority domains to operate on")]
            [Parameter("*")]
            [Parameter("n")]
            [Parameter("nsid")]
            public StringCollection Authority { get; set; }

        }

        /// <summary>
        /// Add the assigning authority
        /// </summary>
        public class AddIdentityDomainParams : IdentityDomainParamBase
        {
            /// <summary>
            /// Gets or sets the OID
            /// </summary>
            [Parameter("o")]
            [Description("The OID of the assigning authority")]
            public String Oid { get; set; }

            /// <summary>
            /// Gets or sets the URL
            /// </summary>
            [Parameter("u")]
            [Description("The URL of the assigning authority")]
            public String Url { get; set; }

            /// <summary>
            /// Gets or sets the description
            /// </summary>
            [Parameter("d")]
            [Description("Sets the description")]
            public String Name { get; set; }

            /// <summary>
            /// Gets or sets the assigner
            /// </summary>
            [Parameter("a")]
            [Description("The application which may assign identities")]
            public String Assigner { get; set; }

            /// <summary>
            /// Gets or sets the scope
            /// </summary>
            [Parameter("s")]
            [Description("The scope of the identity (Patient, Provider, Person, etc.)")]
            public StringCollection Scope { get; set; }

            /// <summary>
            /// Indicates the AA is uuq
            /// </summary>
            [Parameter("q")]
            [Description("Inidicate the authority is unique")]
            public bool Unique { get; set; }
        }

        /// <summary>
        /// Add the assigning authority
        /// </summary>
        public class ListAssigningAuthorityParams : IdentityDomainParamBase
        {
            /// <summary>
            /// Gets or sets the OID
            /// </summary>
            [Parameter("o")]
            [Description("The OID of the assigning authority")]
            public String Oid { get; set; }

            /// <summary>
            /// Gets or sets the URL
            /// </summary>
            [Parameter("u")]
            [Description("The URL of the assigning authority")]
            public String Url { get; set; }

            /// <summary>
            /// Gets or sets the description
            /// </summary>
            [Parameter("d")]
            [Description("Sets the description")]
            public String Name { get; set; }

        }

        /// <summary>
        /// Lists all assigning authorities
        /// </summary>
        [AdminCommand("aa.list", "Query assigning authorties/identity domains")]
        [Description("This command will list all registered identity domains")]
        public static void ListAssigningAuthority(ListAssigningAuthorityParams parms)
        {
            IEnumerable<IdentityDomain> auths = null;
            if (!String.IsNullOrEmpty(parms.Name))
                auths = m_amiClient.GetAssigningAuthorities(o => o.Name.Contains(parms.Name)).CollectionItem.OfType<IdentityDomain>();
            else if (!String.IsNullOrEmpty(parms.Oid))
                auths = m_amiClient.GetAssigningAuthorities(o => o.Oid == parms.Oid).CollectionItem.OfType<IdentityDomain>();
            else if (!String.IsNullOrEmpty(parms.Url))
                auths = m_amiClient.GetAssigningAuthorities(o => o.Url == parms.Url).CollectionItem.OfType<IdentityDomain>();
            else
                auths = m_amiClient.GetAssigningAuthorities(o=>o.CreationTime != null).CollectionItem.OfType<IdentityDomain>();

            DisplayUtil.TablePrint(auths,
                o => o.DomainName,
                o => o.Name,
                o => o.Url,
                o => o.IsUnique);

        }

        /// <summary>
        /// Create a new assigning authority
        /// </summary>
        /// <param name="parms"></param>
        [AdminCommand("aa.add", "Add Assigning Authority application")]
        [Description("This command will create a new assigning authority which can be used to identify external authorities")]
        // [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public static void AddAssigningAuthority(AddIdentityDomainParams parms)
        {

            // First, resolve the assigner 
            SecurityApplicationInfo assigner = null;
            if (!String.IsNullOrEmpty(parms.Assigner))
            {
                assigner = m_amiClient.GetApplications(o => o.Name == parms.Assigner).CollectionItem.FirstOrDefault() as SecurityApplicationInfo;
                if (assigner == null)
                    throw new KeyNotFoundException("Assigner unknown");
            }

            // Scope
            List<Concept> scope = new List<Concept>();
            if (parms.Scope?.Count > 0)
            {
                foreach (var s in parms.Scope)
                {
                    var scp = m_hdsiClient.Query<Concept>(o => o.Mnemonic == s, 0, 1, false).Item.OfType<Concept>().FirstOrDefault();
                    if (scp == null)
                        throw new KeyNotFoundException($"Scope {s} unknown");
                    scope.Add(scp);
                }
            }

            // Construct AA
            foreach (var domainName in parms.Authority)
            {
                var aa = new IdentityDomain(domainName, parms.Name, parms.Oid)
                {
                    Url = parms.Url,
                    AuthorityScope = scope,
                    AssigningAuthority = new List<AssigningAuthority>() { new AssigningAuthority() { AssigningApplication = assigner?.Entity, Reliability = IdentifierReliability.Authoritative } },
                    IsUnique = parms.Unique
                };
                aa = m_amiClient.CreateAssigningAuthority(aa);
                Console.WriteLine("CREATE AUTHORITY {0} = {1}", aa.DomainName, aa.Key);
            }
        }


    }
}
