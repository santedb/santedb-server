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
    /// Represents a security role
    /// </summary>
    [Table("sec_rol_tbl")]
	public class DbSecurityRole : DbNonVersionedBaseData
	{

        /// <summary>
        /// Gets or sets the role id
        /// </summary>
        [Column("rol_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [Column("rol_name")]
		public String Name {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the description.
		/// </summary>
		/// <value>The description.</value>
		[Column("descr")]
		public String Description {
			get;
			set;
		}

	}
}

