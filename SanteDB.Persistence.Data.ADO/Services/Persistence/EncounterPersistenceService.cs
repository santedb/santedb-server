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
using SanteDB.Core.Model.DataTypes;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using System.Diagnostics.Tracing;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Persistence class which persists encounters
    /// </summary>
    public class EncounterPersistenceService : ActDerivedPersistenceService<Core.Model.Acts.PatientEncounter, DbPatientEncounter>
    {

        /// <summary>
        /// Convert database instance to patient encounter
        /// </summary>
        public Core.Model.Acts.PatientEncounter ToModelInstance(DbPatientEncounter dbEnc, DbActVersion actVersionInstance, DbAct actInstance, DataContext context)
        {
            var retVal = m_actPersister.ToModelInstance<Core.Model.Acts.PatientEncounter>(actVersionInstance, actInstance, context);
            if (retVal == null) return null;
            else if (dbEnc == null)
            {
                this.m_tracer.TraceEvent(EventLevel.Warning, "ENC is missing ENC data: {0}", actInstance.Key);
                return null;
            }

            if (dbEnc.DischargeDispositionKey != null)
                retVal.DischargeDispositionKey = dbEnc.DischargeDispositionKey;
            return retVal;
        }

        /// <summary>
        /// Insert the patient encounter
        /// </summary>
        public override Core.Model.Acts.PatientEncounter InsertInternal(DataContext context, Core.Model.Acts.PatientEncounter data)
        {
            if(data.DischargeDisposition != null) data.DischargeDisposition = data.DischargeDisposition?.EnsureExists(context) as Concept;
            data.DischargeDispositionKey = data.DischargeDisposition?.Key ?? data.DischargeDispositionKey;
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Updates the specified data
        /// </summary>
        public override Core.Model.Acts.PatientEncounter UpdateInternal(DataContext context, Core.Model.Acts.PatientEncounter data)
        {
            if (data.DischargeDisposition != null) data.DischargeDisposition = data.DischargeDisposition?.EnsureExists(context) as Concept;
            data.DischargeDispositionKey = data.DischargeDisposition?.Key ?? data.DischargeDispositionKey;
            return base.UpdateInternal(context, data);
        }
    }
}