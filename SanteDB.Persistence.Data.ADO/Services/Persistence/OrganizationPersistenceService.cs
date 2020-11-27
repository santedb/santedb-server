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
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents an organization persistence service
    /// </summary>
    public class OrganizationPersistenceService : EntityDerivedPersistenceService<Core.Model.Entities.Organization, DbOrganization>
    {
       
        /// <summary>
        /// Model instance
        /// </summary>
        public Core.Model.Entities.Organization ToModelInstance(DbOrganization orgInstance, DbEntityVersion dbEntityVersion, DbEntity dbEntity, DataContext context)
        {
            var retVal = m_entityPersister.ToModelInstance<Core.Model.Entities.Organization>(dbEntityVersion, dbEntity, context);
            if (retVal == null) return null;
            retVal.IndustryConceptKey = orgInstance?.IndustryConceptKey;
            return retVal;
        }

        /// <summary>
        /// Insert the organization
        /// </summary>
        public override Core.Model.Entities.Organization InsertInternal(DataContext context, Core.Model.Entities.Organization data)
        {
            // ensure industry concept exists
            if(data.IndustryConcept != null) data.IndustryConcept = data.IndustryConcept?.EnsureExists(context) as Concept;
            data.IndustryConceptKey = data.IndustryConcept?.Key ?? data.IndustryConceptKey;

            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update the organization
        /// </summary>
        public override Core.Model.Entities.Organization UpdateInternal(DataContext context, Core.Model.Entities.Organization data)
        {
            if (data.IndustryConcept != null) data.IndustryConcept = data.IndustryConcept?.EnsureExists(context) as Concept;
            data.IndustryConceptKey = data.IndustryConcept?.Key ?? data.IndustryConceptKey;
            return base.UpdateInternal(context, data);
        }

    }
}
