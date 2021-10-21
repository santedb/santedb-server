﻿/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * Date: 2021-8-27
 */

using SanteDB.OrmLite.Attributes;
using System;

namespace SanteDB.Persistence.Auditing.ADO.Data.Model
{
    /// <summary>
    /// Represents a target object
    /// </summary>
    [Table("aud_obj_dat_tbl")]
    public class DbAuditObjectData
    {
        /// <summary>
        /// Identifier of the object
        /// </summary>
        [Column("id"), PrimaryKey, AutoGenerated]
        public long Key { get; set; }

        /// <summary>
        /// Gets or sets the audit identifier
        /// </summary>
        [Column("obj_id"), ForeignKey(typeof(DbAuditObject), nameof(DbAuditObject.Key))]
        public long ObjectId { get; set; }

        /// <summary>
        /// The identifier of the object
        /// </summary>
        [Column("key")]
        public string Name { get; set; }

        /// <summary>
        /// The object type identifier
        /// </summary>
        [Column("val")]
        public byte[] Value { get; set; }
    }
}