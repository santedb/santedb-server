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
 * User: fyfej
 * Date: 2017-9-1
 */
using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;



namespace SanteDB.Persistence.Data.ADO.Data.Model.Extensibility
{
	/// <summary>
	/// Represents a simpe tag (version independent)
	/// </summary>
	public abstract class DbTag : DbAssociation, IDbBaseData
	{
        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Column("tag_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Created by 
        /// </summary>
        [Column("crt_prov_id"), ForeignKey(typeof(DbSecurityProvenance), nameof(DbSecurityProvenance.Key))]
        public Guid CreatedByKey { get; set; }

        /// <summary>
        /// Creation time
        /// </summary>
        [Column("crt_utc"), AutoGenerated]
        public DateTimeOffset CreationTime { get; set; }

        /// <summary>
        /// Obsoleted by 
        /// </summary>
        [Column("obslt_prov_id"), ForeignKey(typeof(DbSecurityProvenance), nameof(DbSecurityProvenance.Key))]
        public Guid? ObsoletedByKey { get; set; }

        /// <summary>
        /// Gets or sets the obsoletion time
        /// </summary>
        [Column("obslt_utc")]
        public DateTimeOffset? ObsoletionTime { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        [Column("tag_name")]
		public String TagKey {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		[Column("tag_value")]
		public String Value {
			get;
			set;
		}
	}

	/// <summary>
	/// Represents a tag associated with an enttiy
	/// </summary>
	[Table("ent_tag_tbl")]
	public class DbEntityTag : DbTag
	{

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        [Column("ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
        public override Guid SourceKey
        {
            get;
            set;
        }
    }

	/// <summary>
	/// Represents a tag associated with an act
	/// </summary>
	[Table("act_tag_tbl")]
	public class DbActTag : DbTag
	{
        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        [Column("act_id"), ForeignKey(typeof(DbAct), nameof(DbAct.Key))]
        public override Guid SourceKey
        {
            get;
            set;
        }
    }

}

