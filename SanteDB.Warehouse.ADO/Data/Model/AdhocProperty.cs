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

namespace SanteDB.Warehouse.ADO.Data.Model
{
    /// <summary>
    /// Represents an adhoc property
    /// </summary>
    [Table("adhoc_sch_props")]
    public class AdhocProperty
    {

        /// <summary>
        /// Gets or sets the property
        /// </summary>
        [Column("uuid"), NotNull, AutoGenerated]
        public Guid PropertyId { get; set; }

        /// <summary>
        /// Container identifier
        /// </summary>
        [Column("cont_uuid"), ForeignKey(typeof(AdhocProperty), nameof(AdhocProperty.PropertyId))]
        public Guid? ContainerId { get; set; }

        /// <summary>
        /// Schema identifier
        /// </summary>
        [Column("sch_uuid"), NotNull, ForeignKey(typeof(AdhocSchema), nameof(AdhocSchema.SchemaId))]
        public Guid SchemaId { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Column("name"), NotNull]
        public String Name { get; set; }

        /// <summary>
        /// Type identifier
        /// </summary>
        [Column("type"), NotNull]
        public int TypeId { get; set; }

        /// <summary>
        /// Attributes
        /// </summary>
        [Column("attr"), NotNull]
        public int Attributes { get; set; }


    }
}
