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
 * User: fyfej
 * Date: 2017-9-1
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
    /// Audit data
    /// </summary>
    [Table("aud_tbl")]
    [AssociativeTable(typeof(DbAuditActor), typeof(DbAuditActorAssociation))]
    public class DbAuditData
    {
        /// <summary>
        /// The identifier assigned to the audit number
        /// </summary>
        [Column("id"), PrimaryKey, AutoGenerated]
        public Guid Key { get; set; }

        /// <summary>
        /// Outcome of the event
        /// </summary>
        [Column("outc_cs")]
        public int Outcome { get; set; }

        /// <summary>
        /// The action performed
        /// </summary>
        [Column("act_cs")]
        public int ActionCode { get; set; }

        /// <summary>
        /// The type of action performed
        /// </summary>
        [Column("typ_cs")]
        public int EventIdentifier { get; set; }

        /// <summary>
        /// The time of the event
        /// </summary>
        [Column("evt_utc")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The time the data was created
        /// </summary>
        [Column("crt_utc")]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// The event type identifier
        /// </summary>
        [Column("cls_cd_id"), ForeignKey(typeof(DbAuditCode), nameof(DbAuditCode.Key)), AlwaysJoin]
        public Guid EventTypeCode { get; set; }

        
    }
}
