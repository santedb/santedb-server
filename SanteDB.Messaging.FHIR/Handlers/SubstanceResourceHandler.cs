/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;

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
		protected override Substance MapToFhir(Material model, RestOperationContext RestOperationContext)
		{
			var retVal = DataTypeConverter.CreateResource<Substance>(model);

			// Identifiers
			retVal.Identifier = model.Identifiers.Select(o => DataTypeConverter.ToFhirIdentifier<Entity>(o)).ToList();

			// sTatus
			if (model.StatusConceptKey == StatusKeys.Active)
				retVal.Status = SubstanceStatus.Active;
			else if (model.StatusConceptKey == StatusKeys.Nullified)
				retVal.Status = SubstanceStatus.Nullified;
			else if (model.StatusConceptKey == StatusKeys.Obsolete)
				retVal.Status = SubstanceStatus.Inactive;

			// Category and code
			if (model.LoadProperty<Concept>("TypeConcept").ConceptSetsXml.Any(o => o == ConceptSetKeys.VaccineTypeCodes))
				retVal.Category = new SanteDB.Messaging.FHIR.DataTypes.FhirCodeableConcept(new Uri("http://hl7.org/fhir/substance-category"), "drug");
			else
				retVal.Category = new SanteDB.Messaging.FHIR.DataTypes.FhirCodeableConcept(new Uri("http://hl7.org/fhir/substance-category"), "material");

			retVal.Code = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("TypeConcept"), "http://hl7.org/fhir/ValueSet/substance-code");

			retVal.Description = model.LoadCollection<EntityName>("Names").FirstOrDefault(o => o.NameUseKey == NameUseKeys.OfficialRecord)?.LoadCollection<EntityNameComponent>("Components")?.FirstOrDefault()?.Value;

			// TODO: Instance or kind
			if (model.DeterminerConceptKey == DeterminerKeys.Specific)
			{
				var conceptRepo = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();
				retVal.Instance = new List<SanteDB.Messaging.FHIR.Backbone.SubstanceInstance>()
				{
					new SanteDB.Messaging.FHIR.Backbone.SubstanceInstance()
					{
						Expiry = model.ExpiryDate,
						Quantity = new SanteDB.Messaging.FHIR.DataTypes.FhirQuantity() {
							Units = model.QuantityConceptKey.HasValue ? conceptRepo.GetConceptReferenceTerm(model.QuantityConceptKey.Value, "UCUM").Mnemonic : null,
							Value = model.Quantity
						}
					}
				};
			}

			return retVal;
		}

		protected override Material MapToModel(Substance resource, RestOperationContext RestOperationContext)
		{
			throw new NotImplementedException();
		}

        /// <summary>
        /// Get interactions
        /// </summary>
        protected override IEnumerable<InteractionDefinition> GetInteractions()
        {
            return new TypeRestfulInteraction[]
            {
                TypeRestfulInteraction.InstanceHistory,
                TypeRestfulInteraction.Read,
                TypeRestfulInteraction.Search,
                TypeRestfulInteraction.VersionRead,
                TypeRestfulInteraction.Delete
            }.Select(o => new InteractionDefinition() { Type = o });
        }
    }
}