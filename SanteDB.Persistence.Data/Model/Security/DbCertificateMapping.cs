﻿using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Security
{
    /// <summary>
    /// Represents a mapping in the database between a certificate and an identity
    /// </summary>
    [Table("sec_cer_tbl")]
    public class DbCertificateMapping : DbNonVersionedBaseData
    {

        /// <summary>
        /// Gets or sets the primary key
        /// </summary>
        [AutoGenerated, PrimaryKey, Column("cer_id")]
        public override Guid Key { get; set; }

        /// <summary>
        /// Gets or set sthe thumbprint for the certificate for lookup
        /// </summary>
        [Column("x509_thb")]
        public String X509Thumbprint { get; set; }

        /// <summary>
        /// Gets or sets the public key data
        /// </summary>
        [Column("x509_pk")]
        public Byte[] X509PublicKeyData { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the certificate
        /// </summary>
        [Column("exp_utc")]
        public DateTimeOffset Expiration { get; set; }

        /// <summary>
        /// Gets or sets the user identity this is mapped to
        /// </summary>
        [Column("usr_id"), ForeignKey(typeof(DbSecurityUser), nameof(Key))]
        public Guid? SecurityUserKey { get; set; }

        /// <summary>
        /// Gets or sets the application identity this is mapped to if any
        /// </summary>
        [Column("app_id"), ForeignKey(typeof(DbSecurityApplication), nameof(Key))]
        public Guid? SecurityApplicationKey { get; set; }

        /// <summary>
        /// Gets or sets the device that this identity is mapped to
        /// </summary>
        [Column("dev_id"), ForeignKey(typeof(DbSecurityDevice), nameof(Key))]
        public Guid? SecurityDeviceKey { get; set; }

    }
}
