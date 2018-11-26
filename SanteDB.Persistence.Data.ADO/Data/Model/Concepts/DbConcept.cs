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

namespace SanteDB.Persistence.Data.ADO.Data.Model.Concepts
{
    /// <summary>
    /// Physical data layer implemntation of concept
    /// </summary>
    [Table("cd_tbl")]
    [AssociativeTable(typeof(DbConceptSet), typeof(DbConceptSetConceptAssociation))]
	public class DbConcept : DbIdentified, IDbReadonly
	{

		/// <summary>
		/// Gets or sets whether the object is a system concept or not
		/// </summary>
		[Column("is_sys")]
		public bool IsReadonly {
			get;
			set;
		}

        /// <summary>
        /// Gets or sets the code identifier
        /// </summary>
        [Column("cd_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }
    }
}

