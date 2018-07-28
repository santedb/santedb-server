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
    /// Represents security user information
    /// </summary>
    [XmlType(nameof(SecurityUserInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(SecurityUserInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityUserInfo))]
    public class SecurityUserInfo //: SecurityEntityInfo<SecurityUser>
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public SecurityUserInfo()
        {

        }

        /// <summary>
        /// Get the security user information
        /// </summary>
        public SecurityUserInfo(SecurityUser user) //: base(user)
        {
            this.Roles = user.Roles.Select(o => o.Name).ToList();
        }

        /// <summary>
        /// Represents the entity
        /// </summary>
        [XmlElement("entity"), JsonProperty("entity")]
        public SecurityUser Entity { get; set; }

        /// <summary>
        /// When true, indicates that the update is for password setting only
        /// </summary>
        [XmlElement("passwordOnly"), JsonProperty("passwordOnly")]
        public bool PasswordOnly { get; set; }

        /// <summary>
        /// Gets or sets the role this user belongs to
        /// </summary>
        [XmlElement("role"), JsonProperty("role")]
        public List<String> Roles { get; set; }
    }
}
