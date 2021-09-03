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
using SanteDB.Core.Model.DataTypes;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Entities;


namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Persistence service for matrials
    /// </summary>
    public class MaterialPersistenceService : EntityDerivedPersistenceService<Core.Model.Entities.Material, DbMaterial>
    {


        public MaterialPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Creates the specified model instance
        /// </summary>
        public TModel ToModelInstance<TModel>(DbMaterial dbMat, DbEntityVersion dbEntVersion, DbEntity dbEnt, DataContext context)
            where TModel : Core.Model.Entities.Material, new()
        {
            var retVal = this.m_entityPersister.ToModelInstance<TModel>(dbEntVersion, dbEnt, context);
            if (retVal == null) return null;

            retVal.ExpiryDate = dbMat.ExpiryDate;
            retVal.IsAdministrative = dbMat.IsAdministrative;
            retVal.Quantity = dbMat.Quantity;
            retVal.QuantityConceptKey = dbMat.QuantityConceptKey;
            retVal.FormConceptKey = dbMat.FormConceptKey;
            return retVal;

        }

        /// <summary>
        /// Insert the material
        /// </summary>
        public override Core.Model.Entities.Material InsertInternal(DataContext context, Core.Model.Entities.Material data)
        {
            if(data.FormConcept != null) data.FormConcept = data.FormConcept?.EnsureExists(context) as Concept;
            if(data.QuantityConcept != null) data.QuantityConcept = data.QuantityConcept?.EnsureExists(context) as Concept;
            data.FormConceptKey = data.FormConcept?.Key ?? data.FormConceptKey;
            data.QuantityConceptKey = data.QuantityConcept?.Key ?? data.QuantityConceptKey;
            return base.InsertInternal(context, data);
        }

        /// <summary>
        /// Update the specified material
        /// </summary>
        public override Core.Model.Entities.Material UpdateInternal(DataContext context, Core.Model.Entities.Material data)
        {
            if (data.FormConcept != null) data.FormConcept = data.FormConcept?.EnsureExists(context) as Concept;
            if (data.QuantityConcept != null) data.QuantityConcept = data.QuantityConcept?.EnsureExists(context) as Concept;
            data.FormConceptKey = data.FormConcept?.Key ?? data.FormConceptKey;
            data.QuantityConceptKey = data.QuantityConcept?.Key ?? data.QuantityConceptKey;
            return base.UpdateInternal(context, data);
        }

    }
}
