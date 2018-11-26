/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-10-25
 */
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

namespace SanteDB.Tools.AdminConsole.Shell.CmdLets
{
    /// <summary>
    /// Represents an application commandlet for adding/removing/updating devices
    /// </summary>
    [AdminCommandlet]
    public static class DeviceCmdlet
    {

        /// <summary>
        /// Base class for user operations
        /// </summary>
        internal class GenericDeviceParms
        {

            /// <summary>
            /// Gets or sets the username
            /// </summary>
            [Description("The identity of the device")]
            [Parameter("*")]
            public StringCollection DeviceId { get; set; }

        }

        // Ami client
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationServiceContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));

        #region Device Add 

        /// <summary>
        /// Parameters for adding devices
        /// </summary>
        internal class AddDeviceParms : GenericDeviceParms
        {
            /// <summary>
            /// The secret of the application
            /// </summary>
            [Description("The device secret to set")]
            [Parameter("s")]
            public string Secret { get; set; }

            /// <summary>
            /// The policies to add 
            /// </summary>
            [Description("The policies to grant device")]
            [Parameter("g")]
            public StringCollection GrantPolicies { get; set; }

            /// <summary>
            /// The policies to deny 
            /// </summary>
            [Description("The policies to deny the device")]
            [Parameter("d")]
            public StringCollection DenyPolicies { get; set; }

        }

        [AdminCommand("devadd", "Add security device")]
        [Description("This command will create a new security device which can be used to access the SanteDB instance")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateApplication)]
        internal static void AddApplication(AddDeviceParms parms)
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
                parms.Secret = BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-", "");
                Console.WriteLine("Device secret: {0}", parms.Secret);
            }

            m_client.CreateDevice(new SecurityDeviceInfo()
            {
                Policies = policies,
                Entity = new Core.Model.Security.SecurityDevice()
                {
                    Name = parms.DeviceId.OfType<String>().First(),
                    DeviceSecret = parms.Secret,
                }
            });

        }
        #endregion
    }
}
