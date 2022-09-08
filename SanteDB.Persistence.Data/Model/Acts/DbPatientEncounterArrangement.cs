﻿/*
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
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Acts
{
    /// <summary>
    /// Represents a specific consideration or arrangement made for the encounter
    /// </summary>
    [Table("pat_enc_arg_tbl")]
    public class DbPatientEncounterArrangement : DbActVersionedAssociation
    {
        /// <summary>
        /// Gets or sets the primary key of the object
        /// </summary>
        [Column("arg_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the type of arrangement
        /// </summary>
        [Column("typ_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid ArrangementTypeKey { get; set; }

        /// <summary>
        /// Gets or sets the start time of the arrangement
        /// </summary>
        [Column("start_utc")]
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the stop time of the arrangement
        /// </summary>
        [Column("stop_utc")]
        public DateTimeOffset? StopTime { get; set; }
    }
}
