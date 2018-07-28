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
        public string Key => this.CorrelationToken.ToString();
    }
}
