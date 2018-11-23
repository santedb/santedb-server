﻿/*
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
 * User: justin
 * Date: 2018-6-22
 */
using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Data.Model.Security
{
    /// <summary>
    /// Represents a session in the database
    /// </summary>
    [Table("sec_ses_tbl")]
    public class DbSession : DbIdentified
    {
        /// <summary>
        /// Gets or sets the key of the session
        /// </summary>
        [Column("ses_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Identifies the not before or creation time of the session
        /// </summary>
        [Column("crt_utc"), NotNull, AutoGenerated]
        public DateTimeOffset NotBefore { get; set; }

        /// <summary>
        /// Identifies the expiration or not after time of the session
        /// </summary>
        [Column("exp_utc"), NotNull]
        public DateTimeOffset NotAfter { get; set; }

        /// <summary>
        /// Identifies the application identifier to which the session belongs
        /// </summary>
        [Column("app_id"), NotNull]
        public Guid ApplicationKey { get; set; }

        /// <summary>
        /// Identifies the user key to which the session belongs
        /// </summary>
        [Column("usr_id")]
        public Guid? UserKey { get; set; }

        /// <summary>
        /// Identifies the refresh token which can be used to refresh the session
        /// </summary>
        [Column("rfrsh_tkn"), NotNull]
        public String RefreshToken { get; set; }

        /// <summary>
        /// Identifies the expiration of the refresh token
        /// </summary>
        [Column("rfrsh_exp_utc"), NotNull]
        public DateTimeOffset RefreshExpiration { get; set; }

        /// <summary>
        /// Identifies the remote endpoint which established the session
        /// </summary>
        [Column("aud"), NotNull]
        public String Audience { get; set; }

        /// <summary>
        /// The device key
        /// </summary>
        [Column("dev_id")]
        public Guid? DeviceKey { get; internal set; }
    }
}
