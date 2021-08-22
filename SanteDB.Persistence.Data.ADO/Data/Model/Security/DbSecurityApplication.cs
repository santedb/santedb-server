﻿/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.OrmLite.Attributes;
using System;



namespace SanteDB.Persistence.Data.ADO.Data.Model.Security
{
    /// <summary>
    /// Security applicationDb Should only be one entry here as well
    /// </summary>
    [Table("sec_app_tbl")]
	public class DbSecurityApplication : DbNonVersionedBaseData
    {

        /// <summary>
        /// Gets or sets the application id
        /// </summary>
        [Column("app_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the public identifier.
        /// </summary>
        /// <value>The public identifier.</value>
        [Column("app_pub_id")]
		public String PublicId {
			get;
			set;
		}

        /// <summary>
        /// Application authentication secret
        /// </summary>
        [Column("app_scrt"), Secret]
        public String Secret { get; set; }

        /// <summary>
        /// Replaces application identifier
        /// </summary>
        [Column("rplc_app_id")]
        public Guid? ReplacesApplicationKey { get; set; }

        /// <summary>
        /// Gets or sets the lockout
        /// </summary>
        [Column("locked")]
        public DateTimeOffset? Lockout { get; set; }

        /// <summary>
        /// Sets whether the lockout was explicitly set
        /// </summary>
        public bool LockoutSpecified { get; set; }

        /// <summary>
        /// Gets or sets the lockout
        /// </summary>
        [Column("fail_auth")]
        public int? InvalidAuthAttempts { get; set; }

        /// <summary>
        /// Gets the last authenticated time
        /// </summary>
        [Column("last_auth_utc")]
        public DateTimeOffset? LastAuthentication { get; set; }
    }
}

