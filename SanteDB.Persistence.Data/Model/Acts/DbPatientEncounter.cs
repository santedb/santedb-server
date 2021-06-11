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
using SanteDB.Core.Model.Constants;
using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.Model.Concepts;
using System;

namespace SanteDB.Persistence.Data.Model.Acts
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
        [JoinFilter(PropertyName = nameof(DbActVersion.ClassConceptKey), Value = ActClassKeyStrings.Encounter)]
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
        /// Identifies the manner in which the patient was discharged
        /// </summary>
        [Column("dsch_dsp_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid DischargeDispositionKey { get; set; }
    }
}
