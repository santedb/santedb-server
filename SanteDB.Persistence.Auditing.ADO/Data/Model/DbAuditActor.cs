﻿/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.OrmLite.Attributes;
using System;

namespace SanteDB.Persistence.Auditing.ADO.Data.Model
{
    /// <summary>
    /// Audit actors
    /// </summary>
    [Table("aud_act_tbl")]
    [AssociativeTable(typeof(DbAuditData), typeof(DbAuditActorAssociation))]
    public class DbAuditActor
    {
        /// <summary>
        /// Identifier for the actor instance
        /// </summary>
        [Column("id"), PrimaryKey, AutoGenerated]
        public Guid Key { get; set; }

        /// <summary>
        /// User identifier
        /// </summary>
        [Column("usr_id"), NotNull]
        public String UserIdentifier { get; set; }

        /// <summary>
        /// Username
        /// </summary>
        [Column("usr_name")]
        public String UserName { get; set; }
        
        /// <summary>
        /// Role code identifier
        /// </summary>
        [Column("rol_cd_id"), ForeignKey(typeof(DbAuditCode), nameof(DbAuditCode.Key))]
        public Guid ActorRoleCode { get; set; }

    }
}
