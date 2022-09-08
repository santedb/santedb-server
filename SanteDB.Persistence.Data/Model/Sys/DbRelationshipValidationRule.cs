﻿/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Sys
{

    /// <summary>
    /// Relationship target type
    /// </summary>
    public enum RelationshipTargetType
    {
        /// <summary>
        /// Entity-Entity
        /// </summary>
        EntityRelationship = 1,
        /// <summary>
        /// Act-Act
        /// </summary>
        ActRelationship = 2,
        /// <summary>
        /// Act-Entity
        /// </summary>
        ActParticipation = 3
    }

    /// <summary>
    /// Generic definition for an entity relationship validation table
    /// </summary>
    [Table("rel_vrfy_systbl")]
    public class DbRelationshipValidationRule
    {
        /// <summary>
        /// Primary key for the validation rule
        /// </summary>
        [Column("rel_vrfy_id"), PrimaryKey, AutoGenerated]
        public Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the relationship type key
        /// </summary>
        [Column("rel_typ_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid RelationshipTypeKey { get; set; }

        /// <summary>
        /// Gets or sets the source classification code key
        /// </summary>
        [Column("src_cls_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid? SourceClassKey { get; set; }

        /// <summary>
        /// Gets or sets the target class code key
        /// </summary>
        [Column("trg_cls_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid? TargetClassKey { get; set; }

        /// <summary>
        /// Gets or sets the description or documentation for the relationship
        /// </summary>
        [Column("err_desc")]
        public String Description { get; set; }

        /// <summary>
        /// Target class
        /// </summary>
        [Column("rel_cls")]
        public RelationshipTargetType RelationshipClassType { get; set; }
 
    }

}