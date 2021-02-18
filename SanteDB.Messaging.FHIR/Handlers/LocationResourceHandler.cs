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
            switch (model.StatusConceptKey.ToString().ToUpper())
            {
                case StatusKeyStrings.Active:
                case StatusKeyStrings.New:
                    retVal.Status = Location.LocationStatus.Active;
                    break;
                case StatusKeyStrings.Cancelled:
                    retVal.Status = Location.LocationStatus.Suspended;
                    break;
                case StatusKeyStrings.Nullified:
                case StatusKeyStrings.Obsolete:
                    retVal.Status = Location.LocationStatus.Inactive;
                    break;
            }

            retVal.Name = model.LoadCollection<EntityName>("Names").FirstOrDefault(o => o.NameUseKey == NameUseKeys.OfficialRecord)?.LoadCollection<EntityNameComponent>("Component")?.FirstOrDefault()?.Value;
            retVal.Alias = model.LoadCollection<EntityName>("Names").Where(o => o.NameUseKey != NameUseKeys.OfficialRecord)?.Select(n => n.LoadCollection<EntityNameComponent>("Component")?.FirstOrDefault()?.Value).ToList();

            // Convert the determiner code
            if (model.DeterminerConceptKey == DeterminerKeys.Described)
                retVal.Mode = Location.LocationMode.Kind;
            else
                retVal.Mode = Location.LocationMode.Instance;

            retVal.Type = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(Place.TypeConcept)), "http://hl7.org/fhir/ValueSet/v3-ServiceDeliveryLocationRoleType") };
            retVal.Telecom = model.LoadCollection<EntityTelecomAddress>("Telecoms").Select(o => DataTypeConverter.ToFhirTelecom(o)).ToList();
            retVal.Address = DataTypeConverter.ToFhirAddress(model.LoadCollection<EntityAddress>("Addresses").FirstOrDefault());

            if (model.GeoTag != null)
                retVal.Position = new Location.PositionComponent()
                {
                    Latitude = (decimal)model.GeoTag.Lat,
                    Longitude = (decimal)model.GeoTag.Lng
                };

            // Part of?
            var parent = model.LoadCollection<EntityRelationship>(nameof(Entity.Relationships)).FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Parent);
            if (parent != null)
                retVal.PartOf = DataTypeConverter.CreateVersionedReference<Location>(parent.LoadProperty<Entity>(nameof(EntityRelationship.TargetEntity)), restOperationContext);

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