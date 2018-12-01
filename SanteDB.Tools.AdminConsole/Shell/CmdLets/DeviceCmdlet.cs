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
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Messaging.AMI.Client;
using SanteDB.Tools.AdminConsole.Attributes;
using SanteDB.Tools.AdminConsole.Util;
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
        private static AmiServiceClient m_client = new AmiServiceClient(ApplicationContext.Current.GetRestClient(Core.Interop.ServiceEndpointType.AdministrationIntegrationService));

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

        [AdminCommand("device.add", "Add security device")]
        [Description("This command will create a new security device which can be used to access the SanteDB instance")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateDevice)]
        internal static void AddDevice(AddDeviceParms parms)
        {
            var policies = new List<SecurityPolicyInfo>();

            if (parms.GrantPolicies?.Count > 0)
                policies = parms.GrantPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o)).ToList();
            if (parms.DenyPolicies?.Count > 0)
                policies = policies.Union(parms.DenyPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o))).ToList();

            policies.ForEach(o => o.Grant = parms.GrantPolicies?.Contains(o.Oid) == true ? Core.Model.Security.PolicyGrantType.Grant : PolicyGrantType.Deny);

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
            Console.WriteLine("CREATE {0}", parms.DeviceId[0]);

        }

        [AdminCommand("device.alter", "Alter security device")]
        [Description("This command will alter an existing security device which can be used to access the SanteDB instance")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateDevice)]
        internal static void AlterDevice(AddDeviceParms parms)
        {

            // Retrieve device
            foreach (var deviceName in parms.DeviceId)
            {
                int tr = 0;
                var device = m_client.Query<SecurityDevice>(o => o.Name == deviceName, 0, 1, out tr).CollectionItem.FirstOrDefault() as SecurityDeviceInfo;

                // Grant policies
                if (parms.GrantPolicies?.Count > 0 || parms.DenyPolicies?.Count > 0)
                {
                    var policies = new List<SecurityPolicyInfo>();

                    if (parms.GrantPolicies?.Count > 0)
                        policies = parms.GrantPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o)).ToList();
                    if (parms.DenyPolicies?.Count > 0)
                        policies = policies.Union(parms.DenyPolicies.OfType<String>().Select(o => m_client.GetPolicies(r => r.Oid == o).CollectionItem.FirstOrDefault()).OfType<SecurityPolicy>().Select(o => new SecurityPolicyInfo(o))).ToList();

                    policies.ForEach(o => o.Grant = parms.GrantPolicies?.Contains(o.Oid) == true ? Core.Model.Security.PolicyGrantType.Grant : PolicyGrantType.Deny);

                    // Altering policies?
                    if (policies.Count != (parms.DenyPolicies?.Count ?? 0) + (parms.GrantPolicies?.Count ?? 0))
                        throw new InvalidOperationException("Could not find one or more policies");

                    device.Policies = policies;
                }

                // Changing secret?
                if (!String.IsNullOrEmpty(parms.Secret))
                {
                    device.Entity.DeviceSecret = parms.Secret;
                }

                m_client.UpdateDevice(device.Entity.Key.Value, device);

                Console.WriteLine("ALTER {0}", device.Entity.Name);
            }
        }

        /// <summary>
        /// User list parameters
        /// </summary>
        internal class DeviceListParams 
        {

            /// <summary>
            /// Locked
            /// </summary>
            [Description("Filter on locked status")]
            [Parameter("l")]
            public bool Locked { get; set; }

            /// <summary>
            /// Locked
            /// </summary>
            [Description("Include non-active devices")]
            [Parameter("a")]
            public bool Active { get; set; }

           
        }


        [AdminCommand("device.list", "List Security Devices")]
        [Description("This command will list all security devices in the SanteDB instance")]
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
        internal static void ListDevices(DeviceListParams parms)
        {
            AmiCollection list = null;
            int tr = 0;
            if (parms.Active)
                list = m_client.Query<SecurityDevice>(o => o.ObsoletionTime.HasValue, 0, 100, out tr);
            else if (parms.Locked)
                list = m_client.Query<SecurityDevice>(o => o.Lockout.HasValue, 0, 100, out tr);
            else
                list = m_client.Query<SecurityDevice>(o => o.ObsoletionTime == null, 0, 100, out tr);

            DisplayUtil.TablePrint(list.CollectionItem.OfType<SecurityDeviceInfo>(),
                new String[]{ "SID", "Name", "Last Auth.", "Lockout", "ILA", "A" },
                new int[] { 38, 24, 22, 22, 4, 2 },
                o => o.Entity.Key,
                o => o.Entity.Name,
                o => o.Entity.LastAuthenticationXml,
                o => o.Entity.LockoutXml,
                o => o.Entity.InvalidAuthAttempts ?? 0,
                o => !o.Entity.ObsoletionTime.HasValue ? "*" : null);

        }

        /// <summary>
        /// User information
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.CreateDevice)]
        [AdminCommand("device.info", "Displays detailed information about the device")]
        [Description("This command will display detailed information about the specified security device account. It will status, and effective policies")]
        internal static void DeviceInfo(GenericDeviceParms parms)
        {

            if (parms.DeviceId == null)
                throw new InvalidOperationException("Must specify a device");

            foreach (var un in parms.DeviceId)
            {
                var device = m_client.GetDevices(o => o.Name == un).CollectionItem.FirstOrDefault() as SecurityDeviceInfo;
                if (device == null)
                    throw new KeyNotFoundException($"Device {un} not found");

                DisplayUtil.PrintPolicies(device,
                    new string[] { "Name", "SID", "Invalid Auth", "Lockout", "Last Auth", "Created", "Updated", "De-Activated" },
                    u => u.Name,
                    u => u.Key,
                    u => u.InvalidAuthAttempts,
                    u => u.LockoutXml,
                    u=>u.LastAuthenticationXml,
                    u => String.Format("{0} ({1})", u.CreationTimeXml, m_client.GetUser(m_client.GetProvenance(u.CreatedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Format("{0} ({1})", u.UpdatedTimeXml, m_client.GetUser(m_client.GetProvenance(u.UpdatedByKey.Value).UserKey.Value).Entity.UserName),
                    u => String.Format("{0} ({1})", u.ObsoletionTimeXml, m_client.GetUser(m_client.GetProvenance(u.ObsoletedByKey.Value).UserKey.Value).Entity.UserName)
                );

            }
        }
        #endregion
    }
}