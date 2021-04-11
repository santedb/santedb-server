﻿using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.PubSub.ADO.Data.Model
{
    /// <summary>
    /// A subscription filter
    /// </summary>
    [Table("sub_flt_tbl")]
    public class DbSubscriptionFilter
    {
        /// <summary>
        /// The sequence identifier
        /// </summary>
        [Column("flt_id"), PrimaryKey, NotNull, AutoGenerated]
        public int? SequenceId { get; set; }
 
        /// <summary>
        /// The subscription key
        /// </summary>
        [Column("sub_id"), NotNull, ForeignKey(typeof(DbSubscription), nameof(DbSubscription.Key))]
        public Guid SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the filter
        /// </summary>
        [Column("flt"), NotNull]
        public String Filter { get; set; }
    }
}
