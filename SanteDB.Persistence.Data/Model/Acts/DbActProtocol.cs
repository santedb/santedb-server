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
using System;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Persistence.Data.Model.Acts
{
    /// <summary>
    /// Represents a link between act and protocol
    /// </summary>
    [Table("act_proto_assoc_tbl")]
    [ExcludeFromCodeCoverage]
    public class DbActProtocol : DbAssociation
    {

        /// <summary>
        /// Gets or sets the key
        /// </summary>
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the protocol key
        /// </summary>
        [Column("proto_id"), ForeignKey(typeof(DbProtocol), nameof(DbProtocol.Key)), PrimaryKey]
        public Guid ProtocolKey { get; set; }

        /// <summary>
        /// Source key
        /// </summary>
        [Column("act_id"), ForeignKey(typeof(DbActVersion), nameof(DbActVersion.Key)), PrimaryKey]
        public override Guid SourceKey { get; set; }

        /// <summary>
        /// Gets or sets the state
        /// </summary>
        [Column("state_dat")]
        public byte[] State { get; set; }

        /// <summary>
        /// Sequence
        /// </summary>
        [Column("seq"), NotNull]
        public int Sequence { get; set; }

        /// <summary>
        /// Gets or sets the complete flag
        /// </summary>
        [Column("is_compl")]
        public bool IsComplete { get; set; }

        /// <summary>
        /// Gets or sets the version of the protocol used
        /// </summary>
        [Column("ver")]
        public string Version { get; set; }
    }
}
