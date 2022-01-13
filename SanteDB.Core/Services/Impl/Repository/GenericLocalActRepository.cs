/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2021-8-27
 */
using SanteDB.Core;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Server.Core.Services.Impl
{

    /// <summary>
    /// Represents an <see cref="IRepositoryService"/> which stores <see cref="Act"/>s and their derivative classes
    /// </summary>
    /// <seealso cref="IRepositoryService{Act}" />
    /// <seealso cref="IRepositoryService{SubstanceAdministration}" />
    /// <seealso cref="IRepositoryService{QuantityObservation}" />
    /// <seealso cref="IRepositoryService{PatientEncounter}" />
    /// <seealso cref="IRepositoryService{CodedObservation}" />
    /// <seealso cref="IRepositoryService{TextObservation}" />
    public class GenericLocalActRepository<TAct> : GenericLocalClinicalDataRepository<TAct>, ICancelRepositoryService<TAct>
        where TAct : Act
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public GenericLocalActRepository(IPolicyEnforcementService policyService, ILocalizationService localizationService, IPrivacyEnforcementService privacyService = null) : base(policyService, localizationService, privacyService)
        {
        }

        /// <inheritdoc/>
        public TAct Cancel(Guid id)
        {
            var act = base.Get(id);
            act.StatusConceptKey = StatusKeys.Cancelled;
            return base.Save(act);
        }

        /// <inheritdoc/>
        public override IEnumerable<TAct> Find(Expression<Func<TAct, bool>> query, int offset, int? count, out int totalResults, Guid queryId, params ModelSort<TAct>[] orderBy)
        {
            return base.Find(query, offset, count, out totalResults, queryId, orderBy);
        }

        /// <inheritdoc/>
        public override IEnumerable<TAct> Find(Expression<Func<TAct, bool>> query)
        {
            return base.Find(query);
        }

        /// <inheritdoc/>
        public override IEnumerable<TAct> Find(Expression<Func<TAct, bool>> query, int offset, int? count, out int totalResults, params ModelSort<TAct>[] orderBy)
        {
            return base.Find(query, offset, count, out totalResults, orderBy);
        }

        /// <inheritdoc/>
        public override IEnumerable<TAct> FindFast(Expression<Func<TAct, bool>> query, int offset, int? count, out int totalResults, Guid queryId)
        {
            return base.FindFast(query, offset, count, out totalResults, queryId);
        }

        /// <inheritdoc/>
        public override TAct Get(Guid key)
        {
            return base.Get(key);
        }

        /// <inheritdoc/>
        public override TAct Get(Guid key, Guid versionKey)
        {
            return base.Get(key, versionKey);
        }

        /// <inheritdoc/>
        public override TAct Insert(TAct entity)
        {
            return base.Insert(entity);
        }

        /// <inheritdoc/>
        public override TAct Nullify(Guid id)
        {
            return base.Nullify(id);
        }

        /// <inheritdoc/>
        public override TAct Obsolete(Guid key)
        {
            return base.Obsolete(key);
        }

        /// <inheritdoc/>
        public override TAct Save(TAct data)
        {
            return base.Save(data);
        }

        /// <inheritdoc/>
        public override TAct Validate(TAct data)
        {
            if (data == null)
                throw new ArgumentNullException(this.m_localizationService.FormatString("error.type.ArgumentNullException.param", new
                    {
                        param = nameof(data)
                    }));
            base.Validate(data);

            var userService = ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>();
            var currentUserEntity = userService.GetUserEntity(AuthenticationContext.Current.Principal.Identity);
            if (data.Participations.All(o => o.ParticipationRoleKey != ActParticipationKey.Authororiginator))
                data.Participations.Add(new ActParticipation(ActParticipationKey.Authororiginator, currentUserEntity));

            return data;
        }

    }
}
