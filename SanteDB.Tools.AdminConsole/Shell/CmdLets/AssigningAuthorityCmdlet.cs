using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Messaging.HDSI.Client;
using SanteDB.Tools.AdminConsole.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Tools.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Administrative commandlet for assigning authorities
    /// </summary>
    [AdminCommandlet]
    public static class AssigningAuthorityCmdlet
    {
        // Ami client
        private static AmiServiceClient m_amiClient = new AmiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));
        private static HdsiServiceClient m_hdsiClient = new HdsiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.HealthDataService));

        static AssigningAuthorityCmdlet()
        {
            m_hdsiClient.Client.Requesting += (o, e) => e.Query.Add("_lean", "true");
        }
        /// <summary>
        /// Authority params base
        /// </summary>
        public class AssigningAuthorityParamsBase
        {
            /// <summary>
            /// Gets or sets the authority
            /// </summary>
            [Description("The authority domains to operate on")]
            [Parameter("*")]
            public StringCollection Authority { get; set; }

        }

        /// <summary>
        /// Add the assigning authority
        /// </summary>
        public class AddAssigningAuthorityParams : AssigningAuthorityParamsBase
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
        }

        /// <summary>
        /// Create a new assigning authority
        /// </summary>
        /// <param name="parms"></param>
        [AdminCommand("authority.add", "Add Assigning Authority application")]
        [Description("This command will create a new assigning authority which can be used to identify external authorities")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public static void AddAssigningAuthority(AddAssigningAuthorityParams parms)
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
            if(parms.Scope?.Count > 0)
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
            foreach (var domainName in parms.Authority) {
                var aa = new AssigningAuthority(domainName, parms.Name, parms.Oid)
                {
                    Url = parms.Url,
                    AuthorityScope = scope,
                    AssigningApplication = assigner?.Entity
                };
                aa = m_amiClient.CreateAssigningAuthority(aa);
                Console.WriteLine("CREATE AUTHORITY {0} = {1}", aa.DomainName, aa.Key);
            }
        }


    }
}
