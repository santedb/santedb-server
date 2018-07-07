using Newtonsoft.Json;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents additional information for security role
    /// </summary>
    [XmlType(nameof(SecurityRoleInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityRoleInfo))]
    [XmlRoot("SecurityRole", Namespace = "http://santedb.org/ami")]
    public class SecurityRoleInfo : SecurityEntityInfo<SecurityRole>
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
        public SecurityRoleInfo(SecurityRole role) : base(role)
        {
            this.Users = role.Users.Select(o => o.UserName).ToList();
        }

        /// <summary>
        /// Gets the users in the specified role
        /// </summary>
        [XmlElement("user"), JsonProperty("user")]
        public List<String> Users { get; set; }
    }
}
