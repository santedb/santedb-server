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

namespace SanteDB.Persistence.Data.ADO.Data.Model.Concepts
{
    /// <summary>
    /// Concept reference term link
    /// </summary>
    [Table("cd_ref_term_assoc_tbl")]
    public class DbConceptReferenceTerm : DbConceptVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the primary key
        /// </summary>
        [Column("cd_ref_term_id"), AutoGenerated, PrimaryKey]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the target key
        /// </summary>
        [Column("ref_term_id"), ForeignKey(typeof(DbReferenceTerm), nameof(DbReferenceTerm.Key))]
        public Guid TargetKey { get; set; }

        /// <summary>
        /// Gets or sets the relationship type id
        /// </summary>
        [Column("rel_typ_id")]
        public Guid RelationshipTypeKey { get; set; }
    }
}
