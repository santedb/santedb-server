﻿using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.Model.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Mail
{
    /// <summary>
    /// Represents a mailbox which is owned by a user
    /// </summary>
    [Table("mail_box_tbl")]
    public class DbMailbox : DbBaseData
    {
        /// <summary>
        /// Gets or sets the key of the mailbox
        /// </summary>
        [PrimaryKey, Column("mail_box_id"), AutoGenerated]
        public override Guid Key { get; set; }

        /// <summary>
        /// Owner which owns this key
        /// </summary>
        [Column("own_id"), ForeignKey(typeof(DbSecurityUser), nameof(DbSecurityUser.Key)), NotNull]
        public Guid OwnerKey { get; set; }

        /// <summary>
        /// Gets or sets the name of the mailbox
        /// </summary>
        [Column("name"), NotNull]
        public string Name { get; set; }
    }
}