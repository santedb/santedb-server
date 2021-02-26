﻿/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using SanteDB.Core;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Local batch repository service
    /// </summary>
    public class LocalBatchRepository :
        GenericLocalRepository<Bundle>
	{

        /// <summary>
        /// Creates a new batch repository
        /// </summary>
        public LocalBatchRepository(IPrivacyEnforcementService privacyService = null) : base(privacyService)
        {
        }

        /// <summary>
        /// Find the specified bundle (Not supported)
        /// </summary>
        public override IEnumerable<Bundle> Find(Expression<Func<Bundle, bool>> query)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Find the specfied bundle (not supported)
        /// </summary>
        public override IEnumerable<Bundle> Find(Expression<Func<Bundle, bool>> query, int offset, int? count, out int totalResults, params ModelSort<Bundle>[] orderBy)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the specified bundle (not supported)
        /// </summary>
        public override Bundle Get(Guid key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the specified bundle (not supported)
        /// </summary>
        public override Bundle Get(Guid key, Guid versionKey)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Insert the bundle
        /// </summary>
        public override Bundle Insert(Bundle data)
        {
            // We need permission to insert all of the objects
            foreach(var itm in data.Item)
            {
                var irst = typeof(IRepositoryService<>).MakeGenericType(itm.GetType());
                var irsi = ApplicationServiceContext.Current.GetService(irst);
                if (irsi is ISecuredRepositoryService)
                    (irsi as ISecuredRepositoryService).DemandWrite(itm);
            }
            return base.Insert(data);
        }

        /// <summary>
        /// Obsoleting bundles are not supported
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override Bundle Obsolete(Guid key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Save the specified bundle
        /// </summary>
        public override Bundle Save(Bundle data)
        {
            // We need permission to insert all of the objects
            foreach (var itm in data.Item)
            {
                var irst = typeof(IRepositoryService<>).MakeGenericType(itm.GetType());
                var irsi = ApplicationServiceContext.Current.GetService(irst);
                if (irsi is ISecuredRepositoryService)
                    (irsi as ISecuredRepositoryService).DemandAlter(itm);
            }

            return base.Save(data);
        }
    }
}