﻿/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2019-11-27
 */
using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.Model.Acts;
using SanteDB.Persistence.Data.Model.Entities;
using System;



namespace SanteDB.Persistence.Data.Model.DataType
{
    /// <summary>
    /// Represents an identifier
    /// </summary>
    public abstract class DbIdentifier : DbVersionedAssociation
    {

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [Column("id_val")]
        public String Value
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type identifier.
        /// </summary>
        /// <value>The type identifier.</value>
        [Column("id_typ_id"), ForeignKey(typeof(DbIdentifierType), nameof(DbIdentifierType.Key))]
        public Guid? TypeKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the authority identifier.
        /// </summary>
        /// <value>The authority identifier.</value>
        [Column("aut_id"), ForeignKey(typeof(DbAssigningAuthority), nameof(DbAssigningAuthority.Key)), AlwaysJoin]
        public Guid AuthorityKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the time of issue
        /// </summary>
        [Column("iss_dt")]
        public DateTime? IssueDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration time
        /// </summary>
        [Column("exp_dt")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Gets or sets the check digit
        /// </summary>
        [Column("chk_dgt")]
        public String CheckDigit { get; set; }
    }

    /// <summary>
    /// Entity identifier storage.
    /// </summary>
    [Table("ent_id_tbl")]
    public class DbEntityIdentifier : DbIdentifier
    {

        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Column("ent_id_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets the source key
        /// </summary>
        [Column("ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
        public override Guid SourceKey { get; set; }
    }

    /// <summary>
    /// Act identifier storage.
    /// </summary>
    [Table("act_id_tbl")]
    public class DbActIdentifier : DbIdentifier
    {
        /// <summary>
        /// Gets or sets the act key
        /// </summary>
        [Column("act_id_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the source key
        /// </summary>
        [Column("act_id"), ForeignKey(typeof(DbAct), nameof(DbAct.Key))]
        public override Guid SourceKey { get; set; }
    }
}

