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
using System;



namespace SanteDB.Persistence.Data.ADO.Data.Model.Extensibility
{
	/// <summary>
	/// Extension.
	/// </summary>
	public abstract class DbExtension : DbVersionedAssociation
	{

		/// <summary>
		/// Gets or sets the extension identifier.
		/// </summary>
		/// <value>The extension identifier.</value>
		[Column ("ext_typ_id"), ForeignKey(typeof(DbExtensionType), nameof(DbExtensionType.Key))]
		public Guid ExtensionTypeKey { 
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		[Column ("ext_val")]
		public byte[] Value {
			get;
			set;
		}

        /// <summary>
        /// Gets or sets the display
        /// </summary>
        [Column("ext_disp")]
        public String Display { get; set; }

    }

	/// <summary>
	/// Entity extension.
	/// </summary>
	[Table ("ent_ext_tbl")]
	public class DbEntityExtension : DbExtension
	{
        /// <summary>
        /// Gets or sets the primary key
        /// </summary>
        [Column("ent_ext_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the source identifier.
        /// </summary>
        /// <value>The source identifier.</value>
        [Column("ent_id"), ForeignKey(typeof(DbEntity), nameof(DbEntity.PrivateKey))]
        public override Int32 SourcePrivateKey { get; set; }

        /// <summary>
        /// Source private key
        /// </summary>
        [PublicKeyRef(nameof(SourcePrivateKey))]
        public override Guid SourceKey
        {
            get;
            set;
        }

    }

    /// <summary>
    /// Act extensions
    /// </summary>
    [Table ("act_ext_tbl")]
	public class DbActExtension : DbExtension
	{
        /// <summary>
        /// Get or sets the key
        /// </summary>
        [Column("act_ext_id"), PrimaryKey, AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the source identifier.
        /// </summary>
        /// <value>The source identifier.</value>
        [Column("act_id"), ForeignKey(typeof(DbAct), nameof(DbAct.PrivateKey))]
        public override Int32 SourcePrivateKey { get; set; }

        /// <summary>
        /// Gets the source key
        /// </summary>
        [PublicKeyRef(nameof(SourcePrivateKey))]
        public override Guid SourceKey
        {
            get;
            set;
        }
    }
}

