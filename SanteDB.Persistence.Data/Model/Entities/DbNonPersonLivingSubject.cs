using SanteDB.OrmLite.Attributes;
using SanteDB.Persistence.Data.Model.Concepts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Entities
{
    /// <summary>
    /// Non-person living subject
    /// </summary>
    [Table("non_psn_sub_tbl")]
    public class DbNonPersonLivingSubject : DbEntitySubTable
    {

        /// <summary>
        /// Gets or sets the strain of the non-person living subject
        /// </summary>
        [Column("str_cd_id"), ForeignKey(typeof(DbConcept), nameof(DbConcept.Key))]
        public Guid? StrainKey { get; set; }
    }
}
