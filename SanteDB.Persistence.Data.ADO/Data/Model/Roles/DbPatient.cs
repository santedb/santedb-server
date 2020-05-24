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
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using System;

namespace SanteDB.Persistence.Data.ADO.Data.Model.Roles
{
    /// <summary>
    /// Represents a patient in the SQLite store
    /// </summary>
    [Table("pat_tbl")]
	public class DbPatient : DbPersonSubTable
	{

        /// <summary>
        /// Parent key
        /// </summary>
        [JoinFilter(PropertyName = nameof(DbEntity.ClassConceptKey), Value = EntityClassKeyStrings.Patient)]
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
        /// Gets or sets the gender concept
        /// </summary>
        /// <value>The gender concept.</value>
        [Column("gndr_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
		public Guid GenderConceptKey {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the deceased date.
		/// </summary>
		/// <value>The deceased date.</value>
		[Column("dcsd_utc")]
		public DateTime? DeceasedDate {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the deceased date precision.
		/// </summary>
		/// <value>The deceased date precision.</value>
		[Column("dcsd_prec")]
		public string DeceasedDatePrecision {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the multiple birth order.
		/// </summary>
		/// <value>The multiple birth order.</value>
		[Column("mb_ord")]
		public int? MultipleBirthOrder {
			get;
			set;
		}

        /// <summary>
        /// Gets or sets the marital status code
        /// </summary>
        [Column("mrtl_sts_cd_id")]
        public Guid? MaritalStatusKey { get; set; }

        /// <summary>
        /// Gets or sets the education level key
        /// </summary>
        [Column("edu_lvl_cd_id")]
        public Guid? EducationLevelKey { get; set; }

        /// <summary>
        /// Gets or sets the living arrangement key
        /// </summary>
        [Column("lvn_arg_cd_id")]
        public Guid? LivingArrangementKey { get; set; }

        /// <summary>
        /// Gets or sets the ethnic group code id
        /// </summary>
        [Column("eth_grp_cd_id")]
        public Guid? EthnicGroupCodeKey { get; set; }

      
    }
}

