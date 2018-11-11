/*
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
 * User: fyfej
 * Date: 2017-9-1
 */
using SanteDB.Core.Model.Constants;
using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public override Int32 ParentPrivateKey
        {
            get
            {
                return base.ParentPrivateKey;
            }

            set
            {
                base.ParentPrivateKey = value;
            }
        }

        /// <summary>
        /// Gets or sets the route of administration
        /// </summary>
        [Column("rte_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey))]
        public Guid RouteConceptPrivateKey { get; set; }

        /// <summary>
        /// Route concept key
        /// </summary>
        [PublicKeyRef(nameof(RouteConceptKey))]
        public Guid RouteConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the dose unit
        /// </summary>
        [Column("dos_unt_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey))]
        public Int32 DoseUnitConceptPrivateKey { get; set; }

        /// <summary>
        /// Gets the dose unit public key
        /// </summary>
        [PublicKeyRef(nameof(DoseUnitConceptPrivateKey))]
        public Guid DoseUnitConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the site
        /// </summary>
        [Column("ste_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey))]
        public Int32 SiteConceptPrivateKey { get; set; }

        /// <summary>
        /// Gets the public key for site concept
        /// </summary>
        [PublicKeyRef(nameof(SiteConceptPrivateKey))]
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
