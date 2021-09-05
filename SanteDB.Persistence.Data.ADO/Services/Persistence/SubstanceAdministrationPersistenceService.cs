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
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using System;
using System.Diagnostics.Tracing;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service for substance administrations
    /// </summary>
    public class SubstanceAdministrationPersistenceService : ActDerivedPersistenceService<Core.Model.Acts.SubstanceAdministration,DbSubstanceAdministration>
    {

        public SubstanceAdministrationPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Convert databased model to model
        /// </summary>
        public Core.Model.Acts.SubstanceAdministration ToModelInstance(DbSubstanceAdministration sbadmInstance, DbActVersion actVersionInstance, DbAct actInstance, DataContext context)
        {
            var retVal = m_actPersister.ToModelInstance<Core.Model.Acts.SubstanceAdministration>(actVersionInstance, actInstance, context);
            if (retVal == null) return null;
            else if(sbadmInstance == null)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "SBADM is missing SBADM data: {0}", actInstance.Key);
                return null;
            }

            if (sbadmInstance.DoseUnitConceptKey != null)
                retVal.DoseUnitKey = sbadmInstance.DoseUnitConceptKey;
            if (sbadmInstance.RouteConceptKey != null)
                retVal.RouteKey = sbadmInstance.RouteConceptKey;
            if (sbadmInstance.SiteConceptKey != null)
                retVal.SiteKey = sbadmInstance.SiteConceptKey;

            retVal.DoseQuantity = sbadmInstance.DoseQuantity;
            retVal.SequenceId = (int)sbadmInstance.SequenceId;
            
            return retVal;
        }

        /// <summary>
        /// Insert the specified sbadm
        /// </summary>
        public override Core.Model.Acts.SubstanceAdministration InsertInternal(DataContext context, Core.Model.Acts.SubstanceAdministration data)
        {
            if(data.DoseUnit != null) data.DoseUnit = data.DoseUnit?.EnsureExists(context) as Concept;
            if (data.Route != null) data.Route = data.Route?.EnsureExists(context) as Concept;
            else if(!data.RouteKey.HasValue)
                data.RouteKey = NullReasonKeys.NoInformation;

            // JF: Correct dose unit key
            if (this.m_settingsProvider.GetConfiguration().DataCorrectionKeys.Contains("invalid-sbadm-dose-unit") &&
                data.DoseUnitKey == Guid.Parse("a77b8d83-1cc9-4806-a268-5d1738154afa"))
                data.DoseUnitKey = Guid.Parse("a4fc5c93-31c2-4f87-990e-c5a4e5ea2e76");
            data.DoseUnitKey = data.DoseUnit?.Key ?? data.DoseUnitKey;
            data.RouteKey = data.Route?.Key ?? data.RouteKey;
            return base.InsertInternal(context, data);
        }


        /// <summary>
        /// Insert the specified sbadm
        /// </summary>
        public override Core.Model.Acts.SubstanceAdministration UpdateInternal(DataContext context, Core.Model.Acts.SubstanceAdministration data)
        {
            if (data.DoseUnit != null) data.DoseUnit = data.DoseUnit?.EnsureExists(context) as Concept;
            if (data.Route != null) data.Route = data.Route?.EnsureExists(context) as Concept;
            else
                data.RouteKey = NullReasonKeys.NoInformation;

            data.DoseUnitKey = data.DoseUnit?.Key ?? data.DoseUnitKey;
            data.RouteKey = data.Route?.Key ?? data.RouteKey;
            return base.UpdateInternal(context, data);
        }
    }
}