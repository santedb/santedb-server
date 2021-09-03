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
using SanteDB.Persistence.Data.ADO.Data.Model.DataType;
using System;



namespace SanteDB.Persistence.Data.ADO.Data.Model.Entities
{
    /// <summary>
    /// Represents an entity name related to an entity
    /// </summary>
    [Table("ent_name_tbl")]
	public class DbEntityName : DbEntityVersionedAssociation
    {
        [Column("name_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the use concept.
        /// </summary>
        /// <value>The use concept.</value>
        [Column("use_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
		public Guid UseConceptKey {
			get;
			set;
		}
	}

    /// <summary>
    /// Represents a component of a name
    /// </summary>
    [Table("ent_name_cmp_tbl")]
    public class DbEntityNameComponent : DbGenericNameComponent
    {
        /// <summary>
        /// Gets or sets the linked name
        /// </summary>
        [Column("name_id"), ForeignKey(typeof(DbEntityName), nameof(DbEntityName.Key))]
        public override Guid SourceKey { get; set; }

        /// <summary>
        /// Value of the component
        /// </summary>
        [Column("val_seq_id"), ForeignKey(typeof(DbPhoneticValue), nameof(DbPhoneticValue.SequenceId)), AlwaysJoin]
        public override Decimal ValueSequenceId { get; set; }

        /// <summary>
        /// Auto-generated value sequence
        /// </summary>
        [Column("cmp_seq"), AutoGenerated]
        public decimal? Sequence { get; set; }
    }


}

