using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Messaging.AMI.Client;
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
    /// Represents an application commandlet for adding/removing/updating applications
    /// </summary>
    [AdminCommandlet]
    public static class ApplicationCmdlet
    {

        /// <summary>
        /// Base class for user operations
        /// </summary>
        internal class GenericApplicationParms
        {

            /// <summary>
            /// Gets or sets the username
            /// </summary>
            [Description("The identity of the application")]
            [Parameter("*")]
            public StringCollection ApplictionId { get; set; }

        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));

        #region Application Add 

        /// <summary>
        /// Parameters for adding applications
        /// </summary>
        internal class AddApplicationParms : GenericApplicationParms
        {
            /// <summary>
            /// The secret of the application
            /// </summary>
            [Description("The application secret to set")]
            [Parameter("s")]
            public string Secret { get; set; }

            /// <summary>
            /// The policies to add 
            /// </summary>
            [Description("The policies to grant deny application")]
            [Parameter("g")]
            public StringCollection GrantPolicies { get; set; }

            /// <summary>
            /// The policies to deny 
            /// </summary>
            [Description("The policies to deny the application")]
            [Parameter("d")]
            public StringCollection DenyPolicies { get; set; }

            /// <summary>
            /// The note
            /// </summary>
            [Description("A description/note to add to the application")]
            [Parameter("n")]
            public String Description { get; set; }
        }

        [AdminCommand("appadd", "Add security application")]
        [Description("This command will create a new security application which can be used to access the SanteDB instance")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        internal static void AddApplication(AddApplicationParms parms)
        {
            var policies = new List<SecurityPolicyInfo>();

            if (parms.GrantPolicies?.Count > 0)
                policies = parms.GrantPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Name == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o)).ToList();
            if (parms.DenyPolicies?.Count > 0)
                policies = policies.Union(parms.DenyPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Name == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o))).ToList();

            policies.ForEach(o => o.Grant = parms.GrantPolicies?.Contains(o.Name) == true ? Core.Model.Security.PolicyGrantType.Grant : PolicyGrantType.Deny);

            if (policies.Count != (parms.DenyPolicies?.Count ?? 0) + (parms.GrantPolicies?.Count ?? 0))
                throw new InvalidOperationException("Could not find one or more policies");

            if (String.IsNullOrEmpty(parms.Secret))
            {
                parms.Secret = BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "").Substring(0, 12);
                Console.WriteLine("Application secret: {0}", parms.Secret);
            }

            m_client.CreateApplication(new SecurityApplicationInfo()
            {
                Policies = policies,
                Entity = new Core.Model.Security.SecurityApplication()
                {
                    Name = parms.ApplictionId.OfType<String>().First(),
                    ApplicationSecret = parms.Secret
                }
            });

        }
        #endregion
    }
}
