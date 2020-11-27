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
using RestSrvr;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Facility resource handler
    /// </summary>
    public class LocationResourceHandler : RepositoryResourceHandlerBase<Location, Place>
	{
		/// <summary>
		/// Map the inbound place to a FHIR model
		/// </summary>
		protected override Location MapToFhir(Place model, RestOperationContext restOperationContext)
		{
			Location retVal = DataTypeConverter.CreateResource<Location>(model, restOperationContext);
			retVal.Identifier = model.LoadCollection<EntityIdentifier>("Identifiers").Select(o => DataTypeConverter.ToFhirIdentifier<Entity>(o)).ToList();

			// Map status
			if (model.StatusConceptKey == StatusKeys.Active)
				retVal.Status = LocationStatus.Active;
			else if (model.StatusConceptKey == StatusKeys.Obsolete)
				retVal.Status = LocationStatus.Inactive;
			else
				retVal.Status = LocationStatus.Suspended;

			retVal.Name = model.LoadCollection<EntityName>("Names").FirstOrDefault(o => o.NameUseKey == NameUseKeys.OfficialRecord)?.LoadCollection<EntityNameComponent>("Component")?.FirstOrDefault()?.Value;
			retVal.Alias = model.LoadCollection<EntityName>("Names").Where(o => o.NameUseKey != NameUseKeys.OfficialRecord)?.Select(n => (FhirString)n.LoadCollection<EntityNameComponent>("Component")?.FirstOrDefault()?.Value).ToList();

			// Convert the determiner code
			if (model.DeterminerConceptKey == DeterminerKeys.Described)
				retVal.Mode = LocationMode.Kind;
			else
				retVal.Mode = LocationMode.Instance;

			retVal.Type = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("TypeConcept"), "http://hl7.org/fhir/ValueSet/v3-ServiceDeliveryLocationRoleType");
			retVal.Telecom = model.LoadCollection<EntityTelecomAddress>("Telecoms").Select(o => DataTypeConverter.ToFhirTelecom(o)).ToList();
			retVal.Address = DataTypeConverter.ToFhirAddress(model.LoadCollection<EntityAddress>("Addresses").FirstOrDefault());

            if(model.GeoTag != null)
				retVal.Position = new SanteDB.Messaging.FHIR.Backbone.Position()
				{
					Latitude = (decimal)model.GeoTag.Lat,
					Longitude = (decimal)model.GeoTag.Lng
				};

			// Part of?
			var parent = model.LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Parent);
			if (parent != null)
				retVal.PartOf = DataTypeConverter.CreateReference<Location>(parent.LoadProperty<Entity>("TargetEntity"), restOperationContext);

			return retVal;
		}

        /// <summary>
        /// Map the incoming FHIR reosurce to a MODEL resource
        /// </summary>
        /// <param name="resource">The resource to be mapped</param>
        /// <param name="restOperationContext">The operation context under which this method is being called</param>
        /// <returns></returns>
		protected override Place MapToModel(Location resource, RestOperationContext restOperationContext)
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