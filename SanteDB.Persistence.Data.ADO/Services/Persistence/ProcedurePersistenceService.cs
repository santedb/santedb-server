/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using SanteDB.Core.Model.Query;
using System.Diagnostics.Tracing;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service for substance administrations
    /// </summary>
    public class ProcedurePersistenceService : ActDerivedPersistenceService<Core.Model.Acts.Procedure,DbProcedure>
    {
        public ProcedurePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Convert databased model to model
        /// </summary>
        public Core.Model.Acts.Procedure ToModelInstance(DbProcedure procedureInstance, DbActVersion actVersionInstance, DbAct actInstance, DataContext context)
        {
            var retVal = m_actPersister.ToModelInstance<Core.Model.Acts.Procedure>(actVersionInstance, actInstance, context);
            if (retVal == null) return null;
            else if(procedureInstance == null)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "PROC is missing PROC data: {0}", actInstance.Key);
                return null;
            }

            if (procedureInstance.MethodConceptKey != null)
                retVal.MethodKey = procedureInstance.MethodConceptKey;
            if (procedureInstance.ApproachSiteConceptKey != null)
                retVal.ApproachSiteKey = procedureInstance.ApproachSiteConceptKey;
            if (procedureInstance.TargetSiteConceptKey != null)
                retVal.TargetSiteKey = procedureInstance.TargetSiteConceptKey;

            return retVal;
        }

        /// <summary>
        /// Insert the specified sbadm
        /// </summary>
        public override Core.Model.Acts.Procedure InsertInternal(DataContext context, Core.Model.Acts.Procedure data)
        {
            if (data.Method != null) data.Method = data.Method?.EnsureExists(context) as Concept;
            else if(!data.MethodKey.HasValue)
                data.MethodKey = NullReasonKeys.NoInformation;

            if (data.ApproachSite != null) data.ApproachSite = data.ApproachSite?.EnsureExists(context) as Concept;
            if (data.TargetSite != null) data.TargetSite = data.TargetSite?.EnsureExists(context) as Concept;

            // JF: Correct dose unit key
             data.MethodKey = data.Method?.Key ?? data.MethodKey;
            data.ApproachSiteKey = data.ApproachSite?.Key ?? data.ApproachSiteKey;
            data.TargetSiteKey = data.TargetSite?.Key ?? data.TargetSiteKey;

            return base.InsertInternal(context, data);
        }


        /// <summary>
        /// Insert the specified sbadm
        /// </summary>
        public override Core.Model.Acts.Procedure UpdateInternal(DataContext context, Core.Model.Acts.Procedure data)
        {
            if (data.Method != null) data.Method = data.Method?.EnsureExists(context) as Concept;
            else if (!data.MethodKey.HasValue)
                data.MethodKey = NullReasonKeys.NoInformation;

            if (data.ApproachSite != null) data.ApproachSite = data.ApproachSite?.EnsureExists(context) as Concept;
            if (data.TargetSite != null) data.TargetSite = data.TargetSite?.EnsureExists(context) as Concept;

            // JF: Correct dose unit key
            data.MethodKey = data.Method?.Key ?? data.MethodKey;
            data.ApproachSiteKey = data.ApproachSite?.Key ?? data.ApproachSiteKey;
            data.TargetSiteKey = data.TargetSite?.Key ?? data.TargetSiteKey;

            return base.UpdateInternal(context, data);
        }
    }
}