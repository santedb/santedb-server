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
using System;

namespace SanteDB.Persistence.Data.Model.Concepts
{
    /// <summary>
    /// Concept set
    /// </summary>
    [Table("cd_set_tbl")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DbConceptSet : DbNonVersionedBaseData
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [Column("set_name")]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the mnemonic.
        /// </summary>
        /// <value>The mnemonic.</value>
        [Column("mnemonic")]
        public String Mnemonic
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the oid of the concept set
        /// </summary>
        [Column("oid")]
        public String Oid { get; set; }

        /// <summary>
        /// Gets or sets the url of the concept set
        /// </summary>
        [Column("url")]
        public String Url { get; set; }

        /// <summary>
        /// Gets or sets the id
        /// </summary>
        [Column("set_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }
    }

    /// <summary>
    /// Concept set concept association.
    /// </summary>
    [Table("cd_set_mem_assoc_tbl")]
    public class DbConceptSetConceptAssociation : IDbAssociation
    {
        /// <summary>
        /// The source of this association
        /// </summary>
        [Column("set_id"), ForeignKey(typeof(DbConceptSet), nameof(DbConceptSet.Key)), PrimaryKey]
        public Guid SourceKey { get; set; }

        /// <summary>
        /// Gets or sets the concept identifier.
        /// </summary>
        /// <value>The concept identifier.</value>
        [Column("cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key)), PrimaryKey]
        public Guid ConceptKey { get; set; }

        /// <summary>
        /// Determine equality of this relationship and another
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is DbConceptSetConceptAssociation cso)
            {
                return cso.SourceKey == this.SourceKey &&
                    cso.ConceptKey == this.ConceptKey;
            }
            else
            {
                return base.Equals(obj);
            }
        }
    }
}