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



namespace SanteDB.Persistence.Data.ADO.Data.Model.Security
{
    /// <summary>
    /// Represents a security device. This table should only have one row (the current device)
    /// </summary>
    [Table("sec_dev_tbl")]
	public class DbSecurityDevice : DbBaseData
	{
		
		/// <summary>
		/// Gets or sets the public identifier.
		/// </summary>
		/// <value>The public identifier.</value>
		[Column("dev_pub_id")]
		public String PublicId {
			get;
			set;
		}

        /// <summary>
        /// Device secret
        /// </summary>
        [Column("dev_scrt"), Secret]
        public String DeviceSecret { get; set; }

        /// <summary>
        /// Replaces the specified device identifier
        /// </summary>
        [Column("rplc_dev_id")]
        public Guid? ReplacesDeviceKey { get; set; }

        /// <summary>
        /// Gets or sets the lockout
        /// </summary>
        [Column("locked")]
        public DateTimeOffset? Lockout { get; set; }

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

        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Column("dev_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }
    }
}

