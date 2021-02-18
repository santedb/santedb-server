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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents a medication resource handler
    /// </summary>
    public class MedicationResourceHandler : RepositoryResourceHandlerBase<Medication, ManufacturedMaterial>
	{
		/// <summary>
		/// Map this manufactured material to FHIR
		/// </summary>
		protected override Medication MapToFhir(ManufacturedMaterial model, RestOperationContext restOperationContext)
		{
			var retVal = DataTypeConverter.CreateResource<Medication>(model, restOperationContext);

			// Code of medication code
			retVal.Code = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(Entity.TypeConcept)), "http://snomed.info/sct");
			retVal.Identifier = model.LoadCollection<EntityIdentifier>(nameof(Entity.Identifiers)).Select(o => DataTypeConverter.ToFhirIdentifier(o)).ToList();
			switch (model.StatusConceptKey.ToString().ToUpper())
            {
				case StatusKeyStrings.Active:
				case StatusKeyStrings.New:
					retVal.Status = Medication.MedicationStatusCodes.Active;
					break;
				case StatusKeyStrings.Obsolete:
					retVal.Status = Medication.MedicationStatusCodes.Inactive;
					break;
				case StatusKeyStrings.Nullified:
					retVal.Status = Medication.MedicationStatusCodes.EnteredInError;
					break;
            }

			// Is brand?
			var manufacturer = model.LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.ManufacturedProduct);
			if (manufacturer != null)
				retVal.Manufacturer = DataTypeConverter.CreateVersionedReference<Hl7.Fhir.Model.Organization>(manufacturer.LoadProperty<Entity>(nameof(EntityRelationship.TargetEntity)), restOperationContext);

			// Form
			retVal.Form = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("FormConcept"), "http://hl7.org/fhir/ValueSet/medication-form-codes");
			retVal.Batch = new Medication.BatchComponent()
			{
				LotNumber = model.LotNumber,
				ExpirationDateElement = new FhirDateTime(model.ExpiryDate.Value)
			};

			return retVal;
		}

        /// <summary>
        /// Maps the specified <paramref name="resource"/> to the model type <see cref="ManufacturedMaterial"/>
        /// </summary>
        /// <param name="resource">The model resource to be mapped</param>
        /// <param name="restOperationContext">The operation context this request is executing on</param>
        /// <returns>The converted <see cref="ManufacturedMaterial"/></returns>
		protected override ManufacturedMaterial MapToModel(Medication resource, RestOperationContext restOperationContext)
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