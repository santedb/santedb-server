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
    public class SecurityUserInfo : ISecurityEntityInfo<SecurityUser>
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
            this.Entity = user;
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

        /// <summary>
        /// Get polocies for the user
        /// </summary>
        [XmlElement("policy"), JsonProperty("policy")]
        public List<SecurityPolicyInfo> Policies {
            get
            {
                return this.Entity?.Policies.Select(o => new SecurityPolicyInfo(o)).ToList();
            }
            set {; }
        }

        /// <summary>
        /// Get the key for the object
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public string Key
        {
            get => this.Entity?.Key?.ToString();
            set => this.Entity.Key = Guid.Parse(value);
        }

        /// <summary>
        /// Get the tag
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public string Tag => this.Entity?.Tag;

        /// <summary>
        /// Get the modified on
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public DateTimeOffset ModifiedOn => this.Entity?.ModifiedOn ?? DateTimeOffset.Now;
    }
}
