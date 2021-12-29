using SanteDB.Core.Model.Constants;
using SanteDB.OrmLite.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Model.Acts
{
    /// <summary>
    /// Database table for narrative structure
    /// </summary>
    [Table("nar_tbl")]
    public class DbNarrative : DbActSubTable
    {

        /// <summary>
        /// Parent key
        /// </summary>
        [JoinFilter(PropertyName = nameof(DbActVersion.ClassConceptKey), Value = ActClassKeyStrings.Document)]
        [JoinFilter(PropertyName = nameof(DbActVersion.ClassConceptKey), Value = ActClassKeyStrings.DocumentSection)]
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
        /// The version of the document narrative section
        /// </summary>
        [Column("ver")]
        public string VersionNumber { get; set; }

        /// <summary>
        /// The language code of the document narrative
        /// </summary>
        [Column("lang_cs"), NotNull]
        public String LanguageCode { get; set; }

        /// <summary>
        /// The title of the document narrative
        /// </summary>
        [Column("title"), NotNull]
        public String Title { get; set; }

        /// <summary>
        /// The text of the narrative section
        /// </summary>
        [Column("text"), NotNull]
        public String Text { get; set; }
    }
}
