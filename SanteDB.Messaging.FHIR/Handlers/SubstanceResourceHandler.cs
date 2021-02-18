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
using Hl7.Fhir.Model;
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents a resource handler that can handle substances
    /// </summary>
    public class SubstanceResourceHandler : RepositoryResourceHandlerBase<Substance, Material>
	{
		/// <summary>
		/// Map the substance to FHIR
		/// </summary>
		protected override Substance MapToFhir(Material model, RestOperationContext restOperationContext)
		{
			var retVal = DataTypeConverter.CreateResource<Substance>(model, restOperationContext);

			// Identifiers
			retVal.Identifier = model.Identifiers.Select(o => DataTypeConverter.ToFhirIdentifier<Entity>(o)).ToList();

			// sTatus
			switch(model.StatusConceptKey.ToString().ToUpper())
            {
				case StatusKeyStrings.New:
				case StatusKeyStrings.Active:
					retVal.Status = Substance.FHIRSubstanceStatus.Active;
					break;
				case StatusKeyStrings.Nullified:
					retVal.Status = Substance.FHIRSubstanceStatus.EnteredInError;
					break;
				case StatusKeyStrings.Obsolete:
					retVal.Status = Substance.FHIRSubstanceStatus.Inactive;
					break;
            }

			// Category and code
			retVal.Category = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(Entity.TypeConcept)), "http://terminology.hl7.org/CodeSystem/substance-category", true) };
			retVal.Code = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("TypeConcept"), "http://snomed.info/sct", true);

			retVal.Description = model.LoadCollection<EntityName>("Names").FirstOrDefault(o => o.NameUseKey == NameUseKeys.OfficialRecord)?.LoadCollection<EntityNameComponent>("Components")?.FirstOrDefault()?.Value;

			// TODO: Instance or kind
			if(model.DeterminerConceptKey == DeterminerKeys.Described)
            {
				retVal.Instance = model.GetRelationships().Where(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Instance).Select(s => s.LoadProperty<Material>(nameof(EntityRelationship.TargetEntity))).Select(m => new Substance.InstanceComponent()
				{
					ExpiryElement = new FhirDateTime(m.ExpiryDate.Value),
					Identifier = DataTypeConverter.ToFhirIdentifier( m.GetIdentifiers().FirstOrDefault()),
					Quantity = DataTypeConverter.ToQuantity(m.Quantity, m.LoadProperty<Concept>(nameof(Material.QuantityConcept)))
				}).ToList();
            }
			else if (model.DeterminerConceptKey == DeterminerKeys.Specific)
			{
				var conceptRepo = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();
				retVal.Instance = new List<Substance.InstanceComponent>()
				{
					new Substance.InstanceComponent()
					{
						ExpiryElement = new FhirDateTime(model.ExpiryDate.Value),
						Quantity = DataTypeConverter.ToQuantity(model.Quantity, model.LoadProperty<Concept>(nameof(Material.QuantityConcept)))
					}
				};
			}

			return retVal;
		}
        
        /// <summary>
        /// Maps a FHIR based resource to a model based resource
        /// </summary>
        /// <param name="resource">The resource to be mapped</param>
        /// <param name="restOperationContext">The operation context under which this method is being called</param>
        /// <returns>The mapped material</returns>
		protected override Material MapToModel(Substance resource, RestOperationContext restOperationContext)
		{
			throw new NotImplementedException();
		}

        /// <summary>
        /// Get interactions
        /// </summary>
        protected override IEnumerable<ResourceInteractionComponent> GetInteractions()
        {
            return new TypeRestfulInteraction[]
            {
                TypeRestfulInteraction.HistoryInstance,
                TypeRestfulInteraction.Read,
                TypeRestfulInteraction.SearchType,
                TypeRestfulInteraction.Vread,
                TypeRestfulInteraction.Delete
            }.Select(o => new ResourceInteractionComponent() { Code = o });
        }
    }
}