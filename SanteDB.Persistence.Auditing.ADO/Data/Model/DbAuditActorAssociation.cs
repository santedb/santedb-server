﻿/*
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
 * Date: 2018-6-22
 */
using SanteDB.OrmLite.Attributes;
using System;

namespace SanteDB.Persistence.Auditing.ADO.Data.Model
{
    /// <summary>
    /// Associates the audit actor to audit message
    /// </summary>
    [Table("aud_act_assoc_tbl")]
    public class DbAuditActorAssociation
    {
        /// <summary>
        /// Id of the association
        /// </summary>
        [Column("id"), PrimaryKey, AutoGenerated]
        public Guid Key { get; set; }

        /// <summary>
        /// Audit identifier
        /// </summary>
        [Column("aud_id"), NotNull, ForeignKey(typeof(DbAuditData), nameof(DbAuditData.Key))]
        public Guid SourceKey { get; set; }

        /// <summary>
        /// Actor identifier
        /// </summary>
        [Column("act_id"), NotNull, ForeignKey(typeof(DbAuditActor), nameof(DbAuditActor.Key))]
        public Guid TargetKey { get; set; }

        /// <summary>
        /// True if user is requestor
        /// </summary>
        [Column("is_rqo")]
        public bool UserIsRequestor { get; set; }

        /// <summary>
        /// True if user is requestor
        /// </summary>
        [Column("ap")]
        public String AccessPoint { get; set; }


    }
}
