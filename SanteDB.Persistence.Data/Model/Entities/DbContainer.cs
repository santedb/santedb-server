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
using SanteDB.Core.Model.Constants;
using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Entities
{
    /// <summary>
    /// Represents the entity representation of an object
    /// </summary>
    [Table("cont_ent_tbl")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DbContainer : DbEntitySubTable
    {
        /// <summary>
        /// Parent key filter
        /// </summary>
        [Column("ent_vrsn_id"), ForeignKey(typeof(DbMaterial), nameof(DbMaterial.ParentKey)), PrimaryKey, AlwaysJoin, JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.Container)]
        public override Guid ParentKey
        {
            get
            {
                return base.ParentKey;
            }

            set
            {
                base.ParentKey = value;
            }
        }

        /// <summary>
        /// Gets or sets the capacity of the container
        /// </summary>
        [Column("cap_qty")]
        public decimal? CapacityQuantity { get; set; }

        /// <summary>
        /// Gets or sets the diameter quantity
        /// </summary>
        [Column("dia_qty")]
        public decimal? DiameterQuantity { get; set; }

        /// <summary>
        /// Gets or sets the height quantity
        /// </summary>
        [Column("hgt_qty")]
        public decimal? HeightQuantity { get; set; }

        /// <summary>
        /// The lot number of the container
        /// </summary>
        [Column("lot")]
        public string LotNumber { get; set; }
    }
}
