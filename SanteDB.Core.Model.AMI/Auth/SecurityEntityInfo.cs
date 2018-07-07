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
    /// Represents a serializable policy wrapper for the AMI since policies aren't serialized
    /// </summary>
    [XmlType(Namespace = "http://santedb.org/ami")]
    [JsonObject]
    [XmlRoot("SecurityEntity", Namespace = "http://santedb.org/ami")]
    public class SecurityEntityInfo<TSecurityObject>
        where TSecurityObject : SecurityEntity
    {

        /// <summary>
        /// Default ctor
        /// </summary>
        public SecurityEntityInfo()
        {

        }

        /// <summary>
        /// Creates a new security policy wrapper 
        /// </summary>
        /// <param name="obj">The object being wrapped</param>
        public SecurityEntityInfo(TSecurityObject obj)
        {
            this.Entity = obj;
            this.Policies = obj.Policies.Select(o => new SecurityPolicyInfo(o)).ToList();
        }

        /// <summary>
        /// Gets or sets the entity that is wrapped by this wrapper
        /// </summary>
        [XmlElement("application", typeof(SecurityApplication)),
         XmlElement("device", typeof(SecurityDevice)),
         XmlElement("role", typeof(SecurityRole)), 
        JsonProperty("entity")]
        public TSecurityObject Entity { get; set; }

        /// <summary>
        /// Gets or sets the policies that are to be applied are already applied to the entity
        /// </summary>
        [XmlElement("policy"), JsonProperty("policy")]
        public List<SecurityPolicyInfo> Policies { get; set; }
    }
}
