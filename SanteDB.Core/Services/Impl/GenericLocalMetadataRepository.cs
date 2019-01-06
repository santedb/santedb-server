/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-6-22
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Provides generic basis for metadata editing
    /// </summary>
    public class GenericLocalMetadataRepository<TMetadata> : GenericLocalRepository<TMetadata>
        where TMetadata : IdentifiedData
    {

        protected override string QueryPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMetadata;
        protected override string WritePolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.UnrestrictedMetadata;

        /// <summary>
        /// Finds the specified metadata
        /// </summary>
        public override IEnumerable<TMetadata> Find(Expression<Func<TMetadata, bool>> query, int offset, int? count, out int totalResults, Guid queryId, params ModelSort<TMetadata>[] orderBy)
        {
            return base.Find(query, offset, count, out totalResults, queryId, orderBy);
        }

        /// <summary>
        /// Finds the specified metadata
        /// </summary>
        public override IEnumerable<TMetadata> Find(Expression<Func<TMetadata, bool>> query)
        {
            return base.Find(query);
        }

        /// <summary>
        /// Finds the specified metadata
        /// </summary>
        public override IEnumerable<TMetadata> Find(Expression<Func<TMetadata, bool>> query, int offset, int? count, out int totalResults, params ModelSort<TMetadata>[] orderBy)
        {
            return base.Find(query, offset, count, out totalResults, orderBy);
        }

        /// <summary>
        /// Finds the specified metadata
        /// </summary>
        public override IEnumerable<TMetadata> FindFast(Expression<Func<TMetadata, bool>> query, int offset, int? count, out int totalResults, Guid queryId)
        {
            return base.FindFast(query, offset, count, out totalResults, queryId);
        }

        /// <summary>
        /// Gets the specified metadata
        /// </summary>
        public override TMetadata Get(Guid key)
        {
            return base.Get(key);
        }

        /// <summary>
        /// Gets the specified metadata
        /// </summary>
        public override TMetadata Get(Guid key, Guid versionKey)
        {
            return base.Get(key, versionKey);
        }

        /// <summary>
        /// Inserts the specified metadata
        /// </summary>
        public override TMetadata Insert(TMetadata data)
        {
            return base.Insert(data);
        }

        /// <summary>
        /// obsoletes the specified metadata
        /// </summary>
        public override TMetadata Obsolete(Guid key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Saves the specified metadata
        /// </summary>
        public override TMetadata Save(TMetadata data)
        {
            return base.Save(data);
        }
    }
}