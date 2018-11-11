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
 * Date: 2017-10-30
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
    /// Represents a procedure in the data model
    /// </summary>
    [Table("proc_tbl")]
    public class DbProcedure : DbActSubTable
    {

        /// <summary>
        /// Parent key
        /// </summary>
        [JoinFilter(PropertyName = nameof(DbAct.ClassConceptKey), Value = ActClassKeyStrings.Procedure)]
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
        /// Gets or sets the technique used 
        /// </summary>
        [Column("mth_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey))]
        public Int32 MethodConceptPrivateKey { get; set; }
        
        /// <summary>
        /// Method concept key
        /// </summary>
        [PublicKeyRef(nameof(MethodConceptKey))]
        public Guid? MethodConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the approach body site or system
        /// </summary>
        [Column("apr_ste_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey))]
        public Int32 ApproachSiteConceptPrivateKey { get; set; }

        /// <summary>
        /// Lookup public key
        /// </summary>
        [PublicKeyRef(nameof(ApproachSiteConceptKey))]
        public Guid? ApproachSiteConceptKey { get; set; }

        /// <summary>
        /// Gets or sets the target site code
        /// </summary>
        [Column("trg_ste_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey))]
        public Int32 TargetSiteConceptPrivateKey { get; set; }

        /// <summary>
        /// Gets the target site concept key
        /// </summary>
        [PublicKeyRef(nameof(TargetSiteConceptPrivateKey))]
        public Guid? TargetSiteConceptKey { get; set; }
    }
}
