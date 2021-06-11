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
 * Date: 2019-11-27
 */
using SanteDB.Core;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Server.Core.Services.Impl
{

    /// <summary>
    /// Represents an act repository service.
    /// </summary>
    /// <seealso cref="IActRepositoryService" />
    /// <seealso cref="Services.IRepositoryService{Act}" />
    /// <seealso cref="Services.IRepositoryService{SubstanceAdministration}" />
    /// <seealso cref="Services.IRepositoryService{QuantityObservation}" />
    /// <seealso cref="Services.IRepositoryService{PatientEncounter}" />
    /// <seealso cref="Services.IRepositoryService{CodedObservation}" />
    /// <seealso cref="Services.IRepositoryService{TextObservation}" />
    public class GenericLocalActRepository<TAct> : GenericLocalClinicalDataRepository<TAct>, ICancelRepositoryService<TAct>
        where TAct : Act
    {

        /// <summary>
        /// Creates a new local act repository
        /// </summary>
        public GenericLocalActRepository(IPrivacyEnforcementService privacyService = null) : base(privacyService)
        {
        }

        /// <summary>
        /// Insert or update the specified act
        /// </summary>
        public TAct Cancel(Guid id)
        {
            var act = base.Get(id);
            act.StatusConceptKey = StatusKeys.Cancelled;
            return base.Save(act);
        }

        /// <summary>
        /// Find the specified act
        /// </summary>
        public override IEnumerable<TAct> Find(Expression<Func<TAct, bool>> query, int offset, int? count, out int totalResults, Guid queryId, params ModelSort<TAct>[] orderBy)
        {
            return base.Find(query, offset, count, out totalResults, queryId, orderBy);
        }

        /// <summary>
        /// Find the specified act
        /// </summary>
        public override IEnumerable<TAct> Find(Expression<Func<TAct, bool>> query)
        {
            return base.Find(query);
        }

        /// <summary>
        /// Find the specified act
        /// </summary>
        public override IEnumerable<TAct> Find(Expression<Func<TAct, bool>> query, int offset, int? count, out int totalResults, params ModelSort<TAct>[] orderBy)
        {
            return base.Find(query, offset, count, out totalResults, orderBy);
        }

        /// <summary>
        /// Get the specified act
        /// </summary>
        public override TAct Get(Guid key)
        {
            return base.Get(key);
        }

        /// <summary>
        /// Get the specified act
        /// </summary>
        public override TAct Get(Guid key, Guid versionKey)
        {
            return base.Get(key, versionKey);
        }

        /// <summary>
        /// Insert the specified act
        /// </summary>
        public override TAct Insert(TAct entity)
        {
            return base.Insert(entity);
        }

        /// <summary>
        /// Nullify (invalidate or mark entered in error) the clinical act
        /// </summary>
        public override TAct Nullify(Guid id)
        {
            return base.Nullify(id);
        }

        /// <summary>
        /// Obsolete (mark as no longer valid) the specified act
        /// </summary> 
        public override TAct Obsolete(Guid key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Update clinical act
        /// </summary>
        public override TAct Save(TAct data)
        {
            return base.Save(data);
        }

        /// <summary>
        /// Validates an act.
        /// </summary>
        /// <typeparam name="TAct">The type of the act.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>TAct.</returns>
        /// <exception cref="DetectedIssueException">If there are any validation errors detected.</exception>
        /// <exception cref="System.InvalidOperationException">If the data is null.</exception>
        public override TAct Validate(TAct data)
        {
            if (data == null)
                throw new ArgumentNullException($"{nameof(data)} cannot be null");
            base.Validate(data);

            var userService = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
            var currentUserEntity = userService.GetUserEntity(AuthenticationContext.Current.Principal.Identity);
            if (data.Participations.All(o => o.ParticipationRoleKey != ActParticipationKey.Authororiginator))
                data.Participations.Add(new ActParticipation(ActParticipationKey.Authororiginator, currentUserEntity));

            return data;
        }

    }
}
