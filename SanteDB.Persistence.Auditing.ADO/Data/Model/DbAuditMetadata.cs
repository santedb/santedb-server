﻿/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */
using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Auditing.ADO.Data.Model
{
    /// <summary>
    /// Audit metadata
    /// </summary>
    [Table("aud_meta_tbl")]
    public class DbAuditMetadata 
    {
        /// <summary>
        /// Identifier of the object
        /// </summary>
        [Column("id"), PrimaryKey, AutoGenerated]
        public long Key { get; set; }

        /// <summary>
        /// Gets or sets the audit identifier
        /// </summary>
        [Column("aud_id"), ForeignKey(typeof(DbAuditData), nameof(DbAuditData.Key))]
        public Guid AuditId { get; set; }

        /// <summary>
        /// Metadata key for audits
        /// </summary>
        [Column("attr"), NotNull]
        public int MetadataKey { get; set; }

        /// <summary>
        /// The value of the audit metadata
        /// </summary>
        [Column("val_id"), ForeignKey(typeof(DbAuditMetadataValue), nameof(DbAuditMetadataValue.Key))]
        public long ValueId { get; set; }
    }
}
