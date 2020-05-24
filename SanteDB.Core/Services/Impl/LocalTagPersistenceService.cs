/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using System;
using System.Linq;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Tag persistence service for act
    /// </summary>
    [ServiceProvider("Local Tag Persistence")]
    public class LocalTagPersistenceService : ITagPersistenceService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Local Tag Persistence";
        

        /// <summary>
        /// Save tag
        /// </summary>
        public void Save(Guid sourceKey, ITag tag)
        {
            var cache = ApplicationServiceContext.Current.GetService<IDataCachingService>();

            // Don't persist empty tags
            if ((tag as IdentifiedData)?.IsEmpty() == true || tag.TagKey.StartsWith("$")) return;

            if (tag is EntityTag)
            {
                var idp = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityTag>>();
                int tr = 0;
                var existing = idp.Query(o => o.SourceEntityKey == sourceKey && o.TagKey == tag.TagKey, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
                if (existing != null)
                {
                    existing.Value = tag.Value;
                    if (existing.Value == null)
                        idp.Obsolete(existing, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                    else
                        idp.Update(existing as EntityTag, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                }
                else
                {
                    if (!tag.SourceEntityKey.HasValue)
                        tag.SourceEntityKey = sourceKey;
                    idp.Insert(tag as EntityTag, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                }
            }
            else if (tag is ActTag)
            {
                var idp = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActTag>>();
                int tr = 0;
                var existing = idp.Query(o => o.SourceEntityKey == sourceKey && o.TagKey == tag.TagKey, 0, 1, out tr, AuthenticationContext.Current.Principal).FirstOrDefault();
                tag.SourceEntityKey = tag.SourceEntityKey ?? sourceKey;
                if (existing != null)
                {
                    existing.Value = tag.Value;
                    if (existing.Value == null)
                        idp.Obsolete(existing, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                    else
                        idp.Update(existing as ActTag, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                }
                else
                {
                    if (!tag.SourceEntityKey.HasValue)
                        tag.SourceEntityKey = sourceKey;
                    idp.Insert(tag as ActTag, TransactionMode.Commit, AuthenticationContext.Current.Principal);
                }
            }

            cache.Remove(sourceKey);
            
        }
    }


}
