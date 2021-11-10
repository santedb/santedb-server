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
using SanteDB.Persistence.Data.Model.Concepts;
using System;

namespace SanteDB.Persistence.Data.Model.DataType
{
    /// <summary>
    /// Identifier type table.
    /// </summary>
    [Table("id_typ_tbl")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class DbIdentifierType : DbBaseData
	{
        /// <summary>
        /// Gets or sets the id type
        /// </summary>
        [Column("id_typ_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the type concept identifier.
        /// </summary>
        /// <value>The type concept identifier.</value>
        [Column("typ_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
		public Guid TypeConceptKey {
			get;
			set;
		}

        /// <summary>
        /// Gets or sets the type concept identifier.
        /// </summary>
        /// <value>The type concept identifier.</value>
        [Column("ent_scp_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid? ScopeConceptKey
        {
            get;
            set;
        }
    }
}

