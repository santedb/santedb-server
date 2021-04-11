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
using SanteDB.Persistence.Data.Model.Acts;
using SanteDB.Persistence.Data.Model.Entities;
using System;



namespace SanteDB.Persistence.Data.Model.Extensibility
{
    /// <summary>
    /// Represents note storage
    /// </summary>
    public abstract class DbNote : DbVersionedAssociation
	{
        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Column("note_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
		/// Gets or sets the author identifier.
		/// </summary>
		/// <value>The author identifier.</value>
		[Column("auth_ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
		public Guid AuthorKey {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the text.
		/// </summary>
		/// <value>The text.</value>
		[Column("note_txt")]
		public String Text {
			get;
			set;
		}
	}

	/// <summary>
	/// Entity note.
	/// </summary>
	[Table("ent_note_tbl")]
	public class DbEntityNote : DbNote
	{

        /// <summary>
        /// Gets or sets the source identifier.
        /// </summary>
        /// <value>The source identifier.</value>
        [Column("ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.Key))]
        public override Guid SourceKey
        {
            get;
            set;
        }

    }

    /// <summary>
    /// Act note.
    /// </summary>
    [Table("act_note_tbl")]
	public class DbActNote : DbNote
	{
        /// <summary>
        /// Gets or sets the source identifier.
        /// </summary>
        /// <value>The source identifier.</value>
        [Column("act_id"), ForeignKey(typeof(DbAct), nameof(DbAct.Key))]
        public override Guid SourceKey
        {
            get;
            set;
        }

    }
}

