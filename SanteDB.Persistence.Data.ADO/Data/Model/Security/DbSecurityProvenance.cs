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

namespace SanteDB.Persistence.Data.ADO.Data.Model.Security
{
    /// <summary>
    /// Represents simple provenance
    /// </summary>
    [Table("sec_prov_tbl")]
    public class DbSecurityProvenance : DbIdentified
    {
        /// <summary>
        /// Gets or sets the key field
        /// </summary>
        [Column("prov_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the application id
        /// </summary>
        [Column("app_id"), NotNull, ForeignKey(typeof(DbSecurityApplication), nameof(DbSecurityApplication.Key))]
        public Guid ApplicationKey { get; set; }

        /// <summary>
        /// Gets or sets the user id
        /// </summary>
        [Column("usr_id"), ForeignKey(typeof(DbSecurityUser), nameof(DbSecurityUser.Key))]
        public Guid? UserKey { get; set; }

        /// <summary>
        /// Gets or sets the device key
        /// </summary>
        [Column("dev_id"), ForeignKey(typeof(DbSecurityDevice), nameof(DbSecurityDevice.Key))]
        public Guid? DeviceKey { get; set; }

        /// <summary>
        /// Time that the session was established
        /// </summary>
        [Column("est_utc"), AutoGenerated]
        public DateTimeOffset? Established { get; set; }

        /// <summary>
        /// Gets or sets the session key
        /// </summary>
        [Column("ses_id")]
        public Guid? SessionKey { get; set; }

        /// <summary>
        /// Gets or sets the external id that the sender claims was the provenance user
        /// </summary>
        [Column("ext_id")]
        public Guid? ExternalSecurityObjectRefKey { get; set; }

        /// <summary>
        /// External provenance reference type
        /// </summary>
        [Column("ext_typ")]
        public string ExternalSecurityObjectRefType { get; set; }
    }
}
