using Newtonsoft.Json;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents additional information for security role
    /// </summary>
    [XmlType(nameof(SecurityRoleInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityRoleInfo))]
    [XmlRoot(nameof(SecurityRoleInfo), Namespace = "http://santedb.org/ami")]
    public class SecurityRoleInfo : ISecurityEntityInfo<SecurityRole>
    {
        /// <summary>
        /// Create new security role info
        /// </summary>
        public SecurityRoleInfo()
        {

        }

        /// <summary>
        /// Create new security role information from the specified role
        /// </summary>
        public SecurityRoleInfo(SecurityRole role)
        {
            this.Users = role.Users.Select(o => o.UserName).ToList();
            this.Entity = role;
            this.Policies = role.Policies.Select(o => new SecurityPolicyInfo(o)).ToList();
        }

        /// <summary>
        /// Gets or sets the entity that is wrapped by this wrapper
        /// </summary>
        [XmlElement("entity"), JsonProperty("entity")]
        public SecurityRole Entity { get; set; }

        /// <summary>
        /// Gets or sets the policies that are to be applied are already applied to the entity
        /// </summary>
        [XmlElement("policy"), JsonProperty("policy")]
        public List<SecurityPolicyInfo> Policies { get; set; }

        /// <summary>
        /// Gets the users in the specified role
        /// </summary>
        [XmlElement("user"), JsonProperty("user")]
        public List<String> Users { get; set; }
    }
}
