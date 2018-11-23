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

namespace SanteDB.Persistence.Data.ADO.Data.Model
{

    /// <summary>
    /// Indicates an object can be readonly
    /// </summary>
    public interface IDbReadonly
    {
        /// <summary>
        /// Gets whether the object is readonly 
        /// </summary>
        bool IsReadonly { get; }
    }

    /// <summary>
    /// Represents versioned data
    /// </summary>
    public interface IDbVersionedData : IDbBaseData
    {
        
        /// <summary>
        /// Gets or sets the version identifier
        /// </summary>
        Guid VersionKey { get; set; }

        /// <summary>
        /// Gets or sets the version sequence
        /// </summary>
        Int32? VersionSequenceId { get; set; }

        /// <summary>
        /// Gets or sets teh version of the object this replaces
        /// </summary>
        Guid? ReplacesVersionKey { get; set; }
    }

    /// <summary>
    /// Versioned Database data
    /// </summary>
    public abstract class DbVersionedData : DbBaseData, IDbVersionedData
    {
        /// <summary>
        /// Gets whether the object is readonly 
        /// </summary>
        public virtual bool IsReadonly { get; }

        /// <summary>
        /// Gets or sets the version identifier
        /// </summary>
        [AutoGenerated]
        public abstract Guid VersionKey { get; set; }

        /// <summary>
        /// Gets or sets the version sequence
        /// </summary>
        [Column("vrsn_seq_id"), AutoGenerated]
        public Int32? VersionSequenceId { get; set; }

        /// <summary>
        /// Gets or sets teh version of the object this replaces
        /// </summary>
        [Column("rplc_vrsn_id")]
        public Guid? ReplacesVersionKey { get; set; }
    }

}
