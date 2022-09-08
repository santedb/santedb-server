/*
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
using SanteDB.Persistence.Data.Model.Acts;
using SanteDB.Persistence.Data.Model.Concepts;
using SanteDB.Persistence.Data.Model.Entities;
using System;

namespace SanteDB.Persistence.Data.Model
{
    /// <summary>
    /// Database association
    /// </summary>
    public interface IDbAssociation
    {
        /// <summary>
        /// Gets or sets the source of the association
        /// </summary>
        Guid SourceKey { get; set; }
    }

    /// <summary>
    /// Versioned association
    /// </summary>
    public interface IDbVersionedAssociation : IDbAssociation
    {
        /// <summary>
        /// Gets or sets the version when the relationship is effective
        /// </summary>
        [Column("efft_vrsn_seq_id")]
        Int64 EffectiveVersionSequenceId { get; set; }

        /// <summary>
        /// Gets or sets the verson when the relationship is not effecitve
        /// </summary>
        [Column("obslt_vrsn_seq_id")]
        Int64? ObsoleteVersionSequenceId { get; set; }

        /// <summary>
        /// Idetifies whether the obsoletion time is specifically set
        /// </summary>
        bool ObsoleteVersionSequenceIdSpecified { get; set; }
    }

    /// <summary>
    /// Represents the databased associated entity
    /// </summary>
    public abstract class DbAssociation : DbIdentified, IDbAssociation
    {
        /// <summary>
        /// Gets or sets the key of the item associated with this object
        /// </summary>
        public abstract Guid SourceKey { get; set; }
    }

    /// <summary>
    /// Represents the versioned copy of an association
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public abstract class DbVersionedAssociation : DbAssociation, IDbVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the version when the relationship is effective
        /// </summary>
        [Column("efft_vrsn_seq_id")]
        public Int64 EffectiveVersionSequenceId { get; set; }

        /// <summary>
        /// Gets or sets the verson when the relationship is not effecitve
        /// </summary>
        [Column("obslt_vrsn_seq_id")]
        public Int64? ObsoleteVersionSequenceId { get; set; }

        /// <summary>
        /// Gets whether the obsoletion id is specified
        /// </summary>
        public bool ObsoleteVersionSequenceIdSpecified { get; set; }
    }

    /// <summary>
    /// Represents an act association
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public abstract class DbActAssociation : DbAssociation
    {
        /// <summary>
        /// Gets or sets the source entity id
        /// </summary>
        [Column("act_id"), ForeignKey(typeof(DbAct), nameof(DbAct.Key))]
        public override Guid SourceKey { get; set; }
    }

    /// <summary>
    /// Represents an act association
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public abstract class DbActVersionedAssociation : DbVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the source entity id
        /// </summary>
        [Column("act_id"), ForeignKey(typeof(DbAct), nameof(DbAct.Key))]
        public override Guid SourceKey { get; set; }
    }

    /// <summary>
    /// Represents an act association
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public abstract class DbEntityAssociation : DbAssociation
    {
        /// <summary>
        /// Gets or sets the source entity id
        /// </summary>
        [Column("ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
        public override Guid SourceKey { get; set; }
    }

    /// <summary>
    /// Represents an act association
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public abstract class DbEntityVersionedAssociation : DbVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the source entity id
        /// </summary>
        [Column("ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
        public override Guid SourceKey { get; set; }
    }

    /// <summary>
    /// Represents an concept association
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public abstract class DbConceptVersionedAssociation : DbVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the source entity id
        /// </summary>
        [Column("cd_id"), ForeignKey(typeof(DbConceptVersion), nameof(DbConcept.Key))]
        public override Guid SourceKey { get; set; }
    }
}