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
using SanteDB.Core.Model.Roles;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Practitioner resource handler
    /// </summary>
    public class PractitionerResourceHandler : RepositoryResourceHandlerBase<Practitioner, UserEntity>
    {
        /// <summary>
        /// Get the interactions that this resource handler supports
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

        /// <summary>
        /// Map a user entity to a practitioner
        /// </summary>
        protected override Practitioner MapToFhir(UserEntity model, RestOperationContext restOperationContext)
        {
            // Is there a provider that matches this user?
            var provider = model.LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.AssignedEntity)?.LoadProperty<Provider>("TargetEntity") ;
            var retVal = DataTypeConverter.CreateResource<Practitioner>(model, restOperationContext);

            // Identifiers
            retVal.Identifier = (provider?.Identifiers ?? model.Identifiers)?.Select(o => DataTypeConverter.ToFhirIdentifier(o)).ToList();

            // ACtive
            retVal.Active = model.StatusConceptKey == StatusKeys.Active;

            // Names
            retVal.Name = (provider?.LoadCollection<EntityName>("Names") ?? model.LoadCollection<EntityName>("Names"))?.Select(o => DataTypeConverter.ToFhirHumanName(o)).ToList();

            // Telecoms
            retVal.Telecom = (provider?.LoadCollection<EntityTelecomAddress>("Telecom") ?? model.LoadCollection<EntityTelecomAddress>("Telecom"))?.Select(o => DataTypeConverter.ToFhirTelecom(o)).ToList();

            // Address
            retVal.Address = (provider?.LoadCollection<EntityAddress>("Addresses") ?? model.LoadCollection<EntityAddress>("Addresses"))?.Select(o => DataTypeConverter.ToFhirAddress(o)).ToList();

            // Birthdate
            retVal.BirthDateElement = DataTypeConverter.ToFhirDate(provider?.DateOfBirth ?? model.DateOfBirth);

            var photo = (provider?.LoadCollection<EntityExtension>(nameof(Entity.Extensions)) ?? model.LoadCollection<EntityExtension>(nameof(Entity.Extensions)))?.FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.JpegPhotoExtension);
            if (photo != null)
                retVal.Photo = new List<Attachment>() {
                    new Attachment()
                    {
                        ContentType = "image/jpg",
                        Data = photo.ExtensionValueXml
                    }
                };

            // Load the koala-fications 
            retVal.Qualification = provider?.LoadCollection<Concept>("ProviderSpecialty").Select(o => new Practitioner.QualificationComponent()
            {
                Code = DataTypeConverter.ToFhirCodeableConcept(o)
            }).ToList();

            // Language of communication
            retVal.Communication = (provider?.LoadCollection<PersonLanguageCommunication>(nameof(Core.Model.Entities.Person.LanguageCommunication)) ?? model.LoadCollection<PersonLanguageCommunication>(nameof(Core.Model.Entities.Person.LanguageCommunication)))?.Select(o => new CodeableConcept("http://tools.ietf.org/html/bcp47", o.LanguageCode)).ToList();

            return retVal;
        }

        /// <summary>
        /// Map a practitioner to a user entity
        /// </summary>
        protected override UserEntity MapToModel(Practitioner resource, RestOperationContext restOperationContext)
        {
            throw new NotImplementedException();
        }
    }
}
