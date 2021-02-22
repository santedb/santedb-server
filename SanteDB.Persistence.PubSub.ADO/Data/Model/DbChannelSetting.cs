﻿using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.PubSub.ADO.Data.Model
{
    /// <summary>
    /// Represents a single channel setting
    /// </summary>
    [Table("sub_chnl_set_tbl")]
    public class DbChannelSetting
    {
        /// <summary>
        /// The setting identifier
        /// </summary>
        [Column("set_id"), NotNull, AutoGenerated, PrimaryKey]
        public Decimal SequenceId { get; set; }

        /// <summary>
        /// Gets or sets the channel to which this setting applies
        /// </summary>
        [Column("chnl_id"), NotNull, ForeignKey(typeof(DbChannel), nameof(DbChannel.Key))]
        public Guid ChannelKey { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Column("name"), NotNull]
        public String Name { get; set; }

        /// <summary>
        /// Gets or sets the value
        /// </summary>
        [Column("value"), NotNull]
        public String Value { get; set; }

    }
}