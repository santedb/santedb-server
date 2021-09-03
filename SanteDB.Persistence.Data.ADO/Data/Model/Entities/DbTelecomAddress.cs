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
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using System;



namespace SanteDB.Persistence.Data.ADO.Data.Model.Entities
{
    /// <summary>
    /// Represents a telecommunications address
    /// </summary>
    [Table("ent_tel_tbl")]
    public class DbTelecomAddress : DbEntityVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the primary key
        /// </summary>
        [Column("tel_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the telecom use.
        /// </summary>
        /// <value>The telecom use.</value>
        [Column("use_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid TelecomUseKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the telecom type concept
        /// </summary>
        [Column("typ_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid? TypeConceptKey
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Column("tel_val")]
        public String Value
        {
            get;
            set;
        }

    }
}

