using SanteDB.Core.Model.Constants;
using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Entities
{
    /// <summary>
    /// Represents the entity representation of an object
    /// </summary>
    [Table("cont_ent_tbl")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class DbContainer : DbEntitySubTable
    {
        /// <summary>
        /// Parent key filter
        /// </summary>
        [Column("ent_vrsn_id"), ForeignKey(typeof(DbMaterial), nameof(DbMaterial.ParentKey)), PrimaryKey, AlwaysJoin, JoinFilter(PropertyName = nameof(DbEntityVersion.ClassConceptKey), Value = EntityClassKeyStrings.Container)]
        public override Guid ParentKey
        {
            get
            {
                return base.ParentKey;
            }

            set
            {
                base.ParentKey = value;
            }
        }

        /// <summary>
        /// Gets or sets the capacity of the container
        /// </summary>
        [Column("cap_qty")]
        public decimal? CapacityQuantity { get; set; }

        /// <summary>
        /// Gets or sets the diameter quantity
        /// </summary>
        [Column("dia_qty")]
        public decimal? DiameterQuantity { get; set; }

        /// <summary>
        /// Gets or sets the height quantity
        /// </summary>
        [Column("hgt_qty")]
        public decimal? HeightQuantity { get; set; }

        /// <summary>
        /// The lot number of the container
        /// </summary>
        [Column("lot")]
        public string LotNumber { get; set; }
    }
}
