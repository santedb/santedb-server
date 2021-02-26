/*
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
 * Date: 2020-9-11
 */
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Generic nullifiable local repository
    /// </summary>
    public abstract class GenericLocalRepositoryEx<TModel> : GenericLocalRepository<TModel>, IRepositoryServiceEx<TModel>
        where TModel : IdentifiedData, IHasState
    {

        /// <summary>
        /// Create a new privacy service
        /// </summary>
        public GenericLocalRepositoryEx(IPrivacyEnforcementService privacyService = null) : base(privacyService)
        {

        }

        /// <summary>
        /// Nullify the specified object
        /// </summary>
        public virtual TModel Nullify(Guid id)
        {
            var target = base.Get(id); ;
            target.StatusConceptKey = StatusKeys.Nullified;
            return base.Save(target);
        }

        /// <summary>
        /// Touch the specified object
        /// </summary>
        /// <param name="id">The id of the object to be touched</param>
        /// <returns>The touched object</returns>
        public virtual void Touch(Guid id)
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TModel>>();
            if (persistenceService is IDataPersistenceServiceEx<TModel> exPersistence)
            {
                this.DemandAlter(null);
                exPersistence.Touch(id, TransactionMode.Commit, AuthenticationContext.Current.Principal);
            }
            else
                throw new InvalidOperationException("Repository must support TOUCH");
        }
    }
}