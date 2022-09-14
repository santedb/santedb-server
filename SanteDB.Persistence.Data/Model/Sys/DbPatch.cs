using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Sys
{
    /// <summary>
    /// A representation of the patches applied in the database
    /// </summary>
    [Table("patch_db_systbl")]
    public class DbPatch
    {

        /// <summary>
        /// The patch applied ID
        /// </summary>
        [Column("patch_id"), PrimaryKey]
        public String PatchId { get; set; }

        /// <summary>
        /// The time the patch was applied
        /// </summary>
        [Column("apply_date"), NotNull]
        public DateTimeOffset ApplyDate { get; set; }

        /// <summary>
        /// The name of the patch
        /// </summary>
        [Column("info_name")]
        public String Description { get; set; }
    }
}
