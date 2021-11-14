/*
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
using System;

namespace SanteDB.Persistence.Data.Model.Entities
{
    /// <summary>
    /// Represents a place in the local database
    /// </summary>
    [Table("plc_tbl")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DbPlace : DbEntitySubTable
    {
        /// <summary>
        /// Parent key join
        /// </summary>
        [JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.CityOrTown)]
        [JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.Country)]
        [JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.CountyOrParish)]
        [JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.Place)]
        [JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.ServiceDeliveryLocation)]
        [JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.State)]
        [JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.PrecinctOrBorough)]
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
        /// Identifies whether the place is mobile
        /// </summary>
        [Column("mob_ind")]
        public bool IsMobile { get; set; }
    }
}