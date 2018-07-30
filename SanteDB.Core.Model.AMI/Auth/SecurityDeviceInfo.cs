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
    /// Represents wrapper information for security devices
    /// </summary>
    [XmlType(nameof(SecurityDeviceInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(SecurityDeviceInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityDeviceInfo))]
    public class SecurityDeviceInfo : ISecurityEntityInfo<SecurityDevice>
    {
        /// <summary>
        /// Default CTOR
        /// </summary>
        public SecurityDeviceInfo()
        {

        }

        /// <summary>
        /// Creates a new device info from the specified object
        /// </summary>
        public SecurityDeviceInfo(SecurityDevice device)
        {
            this.Entity = device;
            this.Policies = device.Policies.Select(o => new SecurityPolicyInfo(o)).ToList();
        }

        /// <summary>
        /// Gets or sets the entity that is wrapped by this wrapper
        /// </summary>
        [XmlElement("entity"), JsonProperty("entity")]
        public SecurityDevice Entity { get; set; }

        /// <summary>
        /// Gets or sets the policies that are to be applied are already applied to the entity
        /// </summary>
        [XmlElement("policy"), JsonProperty("policy")]
        public List<SecurityPolicyInfo> Policies { get; set; }

        /// <summary>
        /// Get the key for the object
        /// </summary>
        public string Key
        {
            get => this.Entity?.Key?.ToString();
            set => this.Entity.Key = Guid.Parse(value);
        }

        /// <summary>
        /// Get the tag
        /// </summary>
        public string Tag => this.Entity?.Tag;
    }
}
