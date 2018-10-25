using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.Security;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Tools.AdminConsole.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Tools.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Policy commandlet
    /// </summary>
    [AdminCommandlet]
    public static class PolicyCmdlet
    {

        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));

        /// <summary>
        /// List policy parameters
        /// </summary>
        internal class ListPolicyParms
        {

            /// <summary>
            /// List all with name
            /// </summary>
            [Parameter("n")]
            [Description("List policies with specified name")]
            public String Name { get; set; }

            /// <summary>
            /// List with oid
            /// </summary>
            [Parameter("o")]
            [Description("List policies with specified OID pattern")]
            public String Oid { get; set; }

        }

        /// <summary>
        /// List all policies
        /// </summary>
        [AdminCommand("pollist", "List security policies")]
        [Description("Lists all security policies configured on the server")]
        internal static void ListPolicies(ListPolicyParms parms)
        {

            IEnumerable<SecurityPolicy> policies = null;

            if (!String.IsNullOrEmpty(parms.Name))
                policies = m_client.GetPolicies(o => o.Name.Contains(parms.Name)).CollectionItem.OfType<SecurityPolicy>();
            else if (!String.IsNullOrEmpty(parms.Oid))
                policies = m_client.GetPolicies(o => o.Oid.Contains(parms.Oid)).CollectionItem.OfType<SecurityPolicy>();
            else
                policies = m_client.GetPolicies(o => o.Name != null).CollectionItem.OfType<SecurityPolicy>();

            // Now output
            Console.WriteLine("SID{0}Name{1}Oid", new String(' ', 37), new String(' ', 25));
            foreach(var itm in policies)
            {
                Console.WriteLine("{0}{1}{2}{3}{4}",
                    itm.Key,
                    new String(' ', 4),
                    itm.Name.Length > 20 ? itm.Name.Substring(0, 20) + "...  " : itm.Name,
                    itm.Name.Length < 20 ? new String(' ', 25 - itm.Name.Length) : "",
                    itm.Oid);
            }
        }
    }
}
