/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Model.Acts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence.Acts
{
    /// <summary>
    /// Persistence service which manages acts
    /// </summary>
    public class ActPersistenceService : ActDerivedPersistenceService<Act>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ActPersistenceService(IConfigurationManager configurationManager, ILocalizationService localizationService, IAdhocCacheService adhocCacheService = null, IDataCachingService dataCachingService = null, IQueryPersistenceService queryPersistence = null) : base(configurationManager, localizationService, adhocCacheService, dataCachingService, queryPersistence)
        {
        }


        /// <inheritdoc/>
        /// <remarks>This is not implemented on the act persistence service since there are no
        /// dependent tables</remarks>
        protected override void DoCopyVersionSubTableInternal(DataContext context, DbActVersion newVersion)
        {
            if(this.TryGetSubclassPersister(newVersion.ClassConceptKey, out var provider) && provider is IActDerivedPersistenceService adps)
            {
                adps.DoCopyVersionSubTable(context, newVersion);
            }
        }

        /// <summary>
        /// Insert the model using theproper type of persistence based on class code or class
        /// </summary>
        /// <param name="context">The context where the data is to be inserted</param>
        /// <param name="data">The data to be inserted</param>
        /// <returns>The inserted data</returns>
        protected override Act DoInsertModel(DataContext context, Act data)
        {
            switch (data)
            {
                case Account acct:
                    return acct.GetRelatedPersistenceService().Insert(context, acct);
                case CarePlan cp:
                    return cp.GetRelatedPersistenceService().Insert(context, cp);
                case ControlAct ca:
                    return ca.GetRelatedPersistenceService().Insert(context, ca);
                case FinancialContract fc:
                    return fc.GetRelatedPersistenceService().Insert(context, fc);
                case FinancialTransaction ft:
                    return ft.GetRelatedPersistenceService().Insert(context, ft);
                case InvoiceElement ie:
                    return ie.GetRelatedPersistenceService().Insert(context, ie);
                case Narrative nv:
                    return nv.GetRelatedPersistenceService().Insert(context, nv);
                case QuantityObservation qo:
                    return qo.GetRelatedPersistenceService().Insert(context, qo);
                case CodedObservation co:
                    return co.GetRelatedPersistenceService().Insert(context, co);
                case TextObservation to:
                    return to.GetRelatedPersistenceService().Insert(context, to);
                case PatientEncounter pe:
                    return pe.GetRelatedPersistenceService().Insert(context, pe);
                case Procedure pr:
                    return pr.GetRelatedPersistenceService().Insert(context, pr);
                case SubstanceAdministration sa:
                    return sa.GetRelatedPersistenceService().Insert(context, sa);
                default:
                    if (this.TryGetSubclassPersister(data.ClassConceptKey.GetValueOrDefault(), out var service))
                    {
                        return (Act)service.Insert(context, data);
                    }
                    else
                    {
                        return base.DoInsertModel(context, data);
                    }
            }
        }

        /// <summary>
        /// Update the model using theproper type of persistence based on class code or class
        /// </summary>
        /// <param name="context">The context where the data is to be update</param>
        /// <param name="data">The data to be inserted</param>
        /// <returns>The inserted data</returns>
        protected override Act DoUpdateModel(DataContext context, Act data)
        {
            switch (data)
            {
                case Account acct:
                    return acct.GetRelatedPersistenceService().Update(context, acct);
                case CarePlan cp:
                    return cp.GetRelatedPersistenceService().Update(context, cp);
                case ControlAct ca:
                    return ca.GetRelatedPersistenceService().Update(context, ca);
                case FinancialContract fc:
                    return fc.GetRelatedPersistenceService().Update(context, fc);
                case FinancialTransaction ft:
                    return ft.GetRelatedPersistenceService().Update(context, ft);
                case InvoiceElement ie:
                    return ie.GetRelatedPersistenceService().Update(context, ie);
                case Narrative nv:
                    return nv.GetRelatedPersistenceService().Update(context, nv);
                case QuantityObservation qo:
                    return qo.GetRelatedPersistenceService().Update(context, qo);
                case CodedObservation co:
                    return co.GetRelatedPersistenceService().Update(context, co);
                case TextObservation to:
                    return to.GetRelatedPersistenceService().Update(context, to);
                case PatientEncounter pe:
                    return pe.GetRelatedPersistenceService().Update(context, pe);
                case Procedure pr:
                    return pr.GetRelatedPersistenceService().Update(context, pr);
                case SubstanceAdministration sa:
                    return sa.GetRelatedPersistenceService().Update(context, sa);
                default:
                    if (this.TryGetSubclassPersister(data.ClassConceptKey.GetValueOrDefault(), out var service))
                    {
                        return (Act)service.Update(context, data);
                    }
                    else
                    {
                        return base.DoUpdateModel(context, data);
                    }
            }
        }
    }
}
