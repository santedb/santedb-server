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

namespace SanteDB.Persistence.Data.Model.Security
{
    /// <summary>
    /// Represents a claim on a table
    /// </summary>
    [Table("sec_ses_clm_tbl")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DbSessionClaim : DbIdentified
    {

        /// <summary>
        /// Gets or sets the claim
        /// </summary>
        [Column("clm_id"), AutoGenerated ,PrimaryKey]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the session key
        /// </summary>
        [Column("ses_id"), NotNull, ForeignKey(typeof(DbSession), nameof(DbSession.Key))]
        public Guid SessionKey { get; set; }

        /// <summary>
        /// Gets or sets the claim type
        /// </summary>
        [Column("clm_typ"), NotNull]
        public String ClaimType { get; set; }

        /// <summary>
        /// Gets or sets the claim value
        /// </summary>
        [Column("clm_val"), NotNull]
        public String ClaimValue { get; set; }


    }
}
