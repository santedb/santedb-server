/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;

namespace SanteDB.Persistence.Data.ADO.Data.Model.Entities
{
    /// <summary>
    /// Represents an entity which is used to represent an application
    /// </summary>
    [Table("app_ent_tbl")]
	public class DbApplicationEntity : DbEntitySubTable
    {
		/// <summary>
		/// Gets or sets the security application.
		/// </summary>
		/// <value>The security application.</value>
		[Column("sec_app_id"), ForeignKey(typeof(DbSecurityApplication), nameof(DbSecurityApplication.Key))]
		public Guid SecurityApplicationKey {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name of the software.
		/// </summary>
		/// <value>The name of the software.</value>
		[Column("soft_name")]
		public String SoftwareName {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name of the version.
		/// </summary>
		/// <value>The name of the version.</value>
		[Column("ver_name")]
		public String VersionName {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name of the vendor.
		/// </summary>
		/// <value>The name of the vendor.</value>
		[Column("vnd_name")]
		public String VendorName {
			get;
			set;
		}
	}
}

