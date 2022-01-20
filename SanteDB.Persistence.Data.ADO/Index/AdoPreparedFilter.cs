using SanteDB.Core.Data;
using SanteDB.Core.Model.Query;
using SanteDB.Persistence.Data.ADO.Data.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Index
{
    /// <summary>
    /// Represents a prepared filter index metadata 
    /// </summary>
    internal sealed class AdoPreparedFilter : IPreparedFilter
    {
        /// <summary>
        /// Gets the id of the filter
        /// </summary>
        public Guid Id { get;  }

        /// <summary>
        /// Gets the name of the index
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the target type
        /// </summary>
        public Type TargetType { get; }


        /// <summary>
        /// Gets the status
        /// </summary>
        public PreparedFilterIndexState Status { get;  }

        /// <summary>
        /// Gets the provenance that created
        /// </summary>
        public Guid CreatedByKey { get;  }

        /// <summary>
        /// Get the last updated time
        /// </summary>
        public DateTimeOffset? UpdatedTime { get; }

        /// <summary>
        /// Get the last updated by
        /// </summary>
        public Guid? UpdatedByKey { get; }

        /// <summary>
        /// Get the creation time
        /// </summary>
        public DateTimeOffset CreationTime { get; }

        /// <summary>
        /// Get the last time the index was rebuilt
        /// </summary>
        public DateTimeOffset? LastReindex { get; }

        /// <summary>
        /// Gets the index expression
        /// </summary>
        public Expression IndexExpression { get; }

        /// <summary>
        /// Gets the indexer provider
        /// </summary>
        public string Indexer { get; }

        /// <summary>
        /// Creates a new wrapped prepared filter index
        /// </summary>
        public AdoPreparedFilter(DbPreparedFilterDefinition filterIndexDefinition)
        {
            this.Id = filterIndexDefinition.Key;
            this.CreatedByKey = filterIndexDefinition.CreatedByKey;
            this.CreationTime = filterIndexDefinition.CreationTime;
            this.LastReindex = filterIndexDefinition.LastReindex;
            this.Name = filterIndexDefinition.Name;
            this.Status = filterIndexDefinition.Status;
            this.TargetType = Type.GetType(filterIndexDefinition.TargetType);
            this.UpdatedByKey = filterIndexDefinition.UpdatedByKey;
            this.UpdatedTime = filterIndexDefinition.UpdatedTime;
            this.IndexExpression = QueryExpressionParser.BuildPropertySelector(this.TargetType, filterIndexDefinition.FilterExpression, true);
            this.Indexer = filterIndexDefinition.Indexer;
        }

    }
}
