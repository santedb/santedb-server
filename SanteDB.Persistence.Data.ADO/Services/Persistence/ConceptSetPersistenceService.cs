/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Persistence service for ConceptSets
    /// </summary>
    public class ConceptSetPersistenceService : BaseDataPersistenceService<Core.Model.DataTypes.ConceptSet, DbConceptSet>
	{

        /// <summary>
		/// Converts a <see cref="Data.ConceptSet"/> instance to a <see cref="Core.Model.DataTypes.ConceptSet"/> instance.
		/// </summary>
		/// <param name="dataInstance">The <see cref="Data.ConceptSet"/> instance to convert.</param>
		/// <param name="context">The model data context.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the converted <see cref="Core.Model.DataTypes.ConceptSet"/> instance.</returns>
        public override Core.Model.DataTypes.ConceptSet ToModelInstance(object dataInstance, DataContext context)
        {
	        var conceptSet = dataInstance as DbConceptSet;

	        var retVal = base.ToModelInstance(dataInstance, context);

            if (retVal != null)
            {
                retVal.Mnemonic = conceptSet.Mnemonic;
                retVal.Name = conceptSet.Name;
                retVal.Oid = conceptSet.Oid;
                retVal.Url = conceptSet.Url;
                retVal.ConceptsXml = context.Query<DbConceptSetConceptAssociation>(o => o.ConceptSetKey == retVal.Key).Select(o => o.ConceptKey).ToList();
            }

            return retVal;
        }

        /// <summary>
        /// Inser the specified concept set
        /// </summary>
        public override Core.Model.DataTypes.ConceptSet InsertInternal(DataContext context, Core.Model.DataTypes.ConceptSet data)
        {
            var retVal = base.InsertInternal(context, data);

            // Concept sets 
            if (data.ConceptsXml != null)
                foreach (var i in data.ConceptsXml)
                {
                    //i.EnsureExists(context);
                    context.Insert(new DbConceptSetConceptAssociation() { ConceptKey = i, ConceptSetKey = retVal.Key.Value });
                }
            return retVal;
        }

        /// <summary>
        /// Update the specified conceptset
        /// </summary>
        public override Core.Model.DataTypes.ConceptSet UpdateInternal(DataContext context, Core.Model.DataTypes.ConceptSet data)
        {
            var retVal = base.UpdateInternal(context, data);

            // Concept sets 
            if (data.ConceptsXml != null)
            {
                // Special case m2m
                var existingConceptSets = context.Query<DbConceptSetConceptAssociation>(o => o.ConceptSetKey == retVal.Key);
                // Any new?
                var newConcepts = data.ConceptsXml.Where(o => !existingConceptSets.Select(e => e.ConceptKey).ToList().Contains(o));
                foreach (var i in newConcepts)
                    context.Insert(new DbConceptSetConceptAssociation() { ConceptKey = i, ConceptSetKey = retVal.Key.Value });

                var delConcepts = existingConceptSets.Select(e => e.ConceptKey).ToList().Where(o => !data.ConceptsXml.Exists(c => c == o));
                foreach (var i in delConcepts)
                    context.Delete<DbConceptSetConceptAssociation>(p => p.ConceptKey == i && p.ConceptSetKey == retVal.Key.Value);
            }

            return retVal;
        }
    }
}

