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
using RestSrvr;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;

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
			var retVal = DataTypeConverter.CreateResource<Medication>(model);

			// Code of medication code
			retVal.Code = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("TypeConcept"));

			if (model.StatusConceptKey == StatusKeys.Active)
				retVal.Status = SubstanceStatus.Active;
			else if (model.StatusConceptKey == StatusKeys.Obsolete)
				retVal.Status = SubstanceStatus.Inactive;
			else if (model.StatusConceptKey == StatusKeys.Nullified)
				retVal.Status = SubstanceStatus.Nullified;

			// Is brand?
			retVal.IsBrand = false;
			retVal.IsOverTheCounter = model.Tags.Any(o => o.TagKey == "isOtc");

			var manufacturer = model.LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.WarrantedProduct);
			if (manufacturer != null)
				retVal.Manufacturer = DataTypeConverter.CreateReference<SanteDB.Messaging.FHIR.Resources.Organization>(manufacturer.LoadProperty<Entity>("TargetEntity"), restOperationContext);

			// Form
			retVal.Form = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("FormConcept"), "http://hl7.org/fhir/ValueSet/medication-form-codes");
			retVal.Package = new SanteDB.Messaging.FHIR.Backbone.MedicationPackage();
			retVal.Package.Batch = new SanteDB.Messaging.FHIR.Backbone.MedicationBatch()
			{
				LotNumber = model.LotNumber,
				Expiration = model.ExpiryDate
			};

			// Picture of the object?

			var photo = model.LoadCollection<EntityExtension>("Extensions").FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.JpegPhotoExtension);
			if (photo != null)
				retVal.Image = new SanteDB.Messaging.FHIR.DataTypes.Attachment()
				{
					ContentType = "image/jpg",
					Data = photo.ExtensionValueXml
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