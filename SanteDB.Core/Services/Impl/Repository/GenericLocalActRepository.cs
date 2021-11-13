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
        /// DI constructor
        /// </summary>
        public GenericLocalActRepository(IPolicyEnforcementService policyService, ILocalizationService localizationService, IDataPersistenceService<TAct> dataPersistenceService, IPrivacyEnforcementService privacyService = null) : base(policyService, localizationService, dataPersistenceService, privacyService)
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
                throw new ArgumentNullException(this.m_localizationService.GetString("error.type.ArgumentNullException.param", new
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