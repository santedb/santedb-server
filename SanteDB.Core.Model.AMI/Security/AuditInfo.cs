using MARC.HI.EHRS.SVC.Auditing.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Security
{
    /// <summary>
    /// Represents a simple wrapper for an audit data instance
    /// </summary>
    [XmlType(nameof(AuditInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(AuditInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject]
    public class AuditInfo : AuditData, IAmiIdentified
    {
        /// <summary>
        /// Get the key for this object
        /// </summary>
        public string Key {
            get => this.CorrelationToken.ToString();
            set
            {
                ;
            }
        }

        /// <summary>
        /// Gets the ETag
        /// </summary>
        public string Tag => this.Timestamp.ToString("yyyyMMddHHmmSS");

        /// <summary>
        /// Get the modified on
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public DateTimeOffset ModifiedOn => this.Timestamp;
    }
}
