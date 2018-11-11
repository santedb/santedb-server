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
using System;
using System.Linq;

using System.Collections.Generic;
using SanteDB.OrmLite.Attributes;

namespace SanteDB.Persistence.Data.ADO.Data.Model.Concepts
{
	/// <summary>
	/// Physical data layer implemntation of concept
	/// </summary>
	[Table("cd_vrsn_tbl")]
	public class DbConceptVersion : DbVersionedData, IDbPrivateKey
	{

		/// <summary>
		/// Gets or sets the object mnemonic
		/// </summary>
		/// <value>The mnemonic.</value>
		[Column("mnemonic")]
		public string Mnemonic {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the status concept id
		/// </summary>
		[Column("sts_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey))]
        public Int32 StatusPrivateKey { get; set; }

        /// <summary>
        /// Status key
        /// </summary>
        [PublicKeyRef(nameof(StatusPrivateKey))]
        public Guid StatusKey {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the concept classification
		/// </summary>
		[Column("cls_id"), ForeignKey(typeof(DbConceptClass), nameof(DbConceptClass.Key))]
		public Guid ClassKey {
			get;
			set;
		}

        /// <summary>
        /// The version identifier
        /// </summary>
        [Column("vrsn_uuid"), PrimaryKey, AutoGenerated]
        public override Guid VersionKey { get; set; }

        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [Column("cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.PrivateKey)), AlwaysJoin]
        public Int32 PrivateKey { get; set; }

        /// <summary>
        /// Public key 
        /// </summary>
        [PublicKeyRef(nameof(PrivateKey))]
        public override Guid Key { get; set; }
    }
}

