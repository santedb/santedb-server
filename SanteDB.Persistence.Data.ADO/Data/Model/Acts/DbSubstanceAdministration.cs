/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Model.Constants;
using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using System;

namespace SanteDB.Persistence.Data.ADO.Data.Model.Acts
{
    /// <summary>
    /// Represents storage class for a substance administration
    /// </summary>
    [Table("sub_adm_tbl")]
    public class DbSubstanceAdministration : DbActSubTable
    {

        /// <summary>
        /// Parent key
        /// </summary>
        [JoinFilter(PropertyName = nameof(DbAct.ClassConceptKey), Value = ActClassKeyStrings.SubstanceAdministration)]
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
        /// Gets or sets the route of administration
        /// </summary>
        [Column("rte_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid RouteConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the dose unit
        /// </summary>
        [Column("dos_unt_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid DoseUnitConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the site
        /// </summary>
        [Column("ste_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid SiteConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the dose quantity
        /// </summary>
        [Column("dos_qty")]
        public Decimal DoseQuantity { get; set; }

        /// <summary>
        /// Gets or sets the sequence number
        /// </summary>
        [Column("seq_id")]
        public int SequenceId { get; set; }

    }
}
