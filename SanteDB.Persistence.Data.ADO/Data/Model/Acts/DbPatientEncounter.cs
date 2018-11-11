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
    /// Represents storage class for a patient encounter
    /// </summary>
    [Table("pat_enc_tbl")]
    public class DbPatientEncounter : DbActSubTable
    {

        /// <summary>
        /// Parent key
        /// </summary>
        [JoinFilter(PropertyName = nameof(DbAct.ClassConceptKey), Value = ActClassKeyStrings.Encounter)]
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
        /// Identifies the manner in which the patient was discharged
        /// </summary>
        [Column("dsch_dsp_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey))]
        public Int32 DischargeDispositionPrivateKey { get; set; }

        /// <summary>
        /// Gets the discharge disposition key
        /// </summary>
        [PublicKeyRef(nameof(DischargeDispositionPrivateKey))]
        public Guid DischargeDispositionKey { get; set; }
    }
}
