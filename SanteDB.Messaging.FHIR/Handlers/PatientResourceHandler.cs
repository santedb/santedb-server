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
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.CapabilityStatement;
using DatePrecision = SanteDB.Core.Model.DataTypes.DatePrecision;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents a resource handler which can handle patients.
    /// </summary>
    public class PatientResourceHandler : RepositoryResourceHandlerBase<Patient, Core.Model.Roles.Patient>, IBundleResourceHandler
	{

		/// <summary>
		/// Map a patient object to FHIR.
		/// </summary>
		/// <param name="model">The patient to map to FHIR</param>
        /// <param name="restOperationContext">The current REST operation context</param>
		/// <returns>Returns the mapped FHIR resource.</returns>
		protected override Patient MapToFhir(Core.Model.Roles.Patient model, RestOperationContext restOperationContext)
		{
			var retVal = DataTypeConverter.CreateResource<Patient>(model, restOperationContext);
			retVal.Active = model.StatusConceptKey == StatusKeys.Active || model.StatusConceptKey == StatusKeys.New;
			retVal.Address = model.GetAddresses().Select(o => DataTypeConverter.ToFhirAddress(o)).ToList();
            if(model.DateOfBirth.HasValue)
                switch(model.DateOfBirthPrecision.GetValueOrDefault())
                {
                    case DatePrecision.Day:
                        retVal.BirthDate = model.DateOfBirth.Value.ToString("yyyy-MM-dd");
                        break;
                    case DatePrecision.Month:
                        retVal.BirthDate = model.DateOfBirth.Value.ToString("yyyy-MM");
                        break;
                    case DatePrecision.Year:
                        retVal.BirthDate = model.DateOfBirth.Value.ToString("yyyy");
                        break;
                }

            // Deceased precision
            if(model.DeceasedDate.HasValue)
            {
                if (model.DeceasedDate == DateTime.MinValue)
                {
                    retVal.Deceased = new FhirBoolean(true);
                }
                else
                {
                    switch (model.DeceasedDatePrecision)
                    {
                        case DatePrecision.Day:
                            retVal.Deceased = new Date(model.DeceasedDate.Value.Year, model.DeceasedDate.Value.Month, model.DeceasedDate.Value.Day);
                            break;
                        case DatePrecision.Month:
                            retVal.Deceased = new Date(model.DeceasedDate.Value.Year, model.DeceasedDate.Value.Month);
                            break;
                        case DatePrecision.Year:
                            retVal.Deceased = new Date(model.DeceasedDate.Value.Year);
                            break;
                        default:
                            retVal.Deceased = new FhirDateTime(model.DeceasedDate.Value);
                            break;
                    }
                }
            }

            if (model.GenderConceptKey.HasValue)
            {
                retVal.Gender = DataTypeConverter.ToFhirEnumeration<AdministrativeGender>(model.LoadProperty<Concept>("GenderConcept"), "http://hl7.org/fhir/administrative-gender");
            }

            retVal.Identifier = model.Identifiers?.Select(o => DataTypeConverter.ToFhirIdentifier(o)).ToList();
            retVal.MultipleBirth = model.MultipleBirthOrder == 0 ? (Element)new FhirBoolean(true) : model.MultipleBirthOrder.HasValue ? new Integer(model.MultipleBirthOrder.Value) : null;
			retVal.Name = model.GetNames().Select(o => DataTypeConverter.ToFhirHumanName(o)).ToList();
			retVal.Telecom = model.GetTelecoms().Select(o => DataTypeConverter.ToFhirTelecom(o)).ToList();
            retVal.Communication = model.GetPersonLanguages().Select(o => DataTypeConverter.ToFhirCommunicationComponent(o)).ToList();
			// TODO: Relationships
			foreach (var rel in model.GetRelationships().Where(o => !o.InversionIndicator))
			{
                // Family member
                if (rel.LoadProperty<Concept>(nameof(EntityRelationship.RelationshipType)).ConceptSetsXml.Contains(ConceptSetKeys.FamilyMember))
                {

                    var contact = new Patient.ContactComponent()
                    {
                        ElementId = $"urn:uuid:{rel.TargetEntity.Key}",
                        Address = DataTypeConverter.ToFhirAddress(rel.TargetEntity.GetAddresses().FirstOrDefault()),
                        Relationship = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(rel.LoadProperty<Concept>(nameof(EntityRelationship.RelationshipType)), "http://terminology.hl7.org/CodeSystem/v2-0131") },
                        Name = DataTypeConverter.ToFhirHumanName(rel.TargetEntity.GetNames().FirstOrDefault()),
                        // TODO: Gender
                        Telecom = rel.TargetEntity.GetTelecoms().Select(t => DataTypeConverter.ToFhirTelecom(t)).ToList()
                    };

                    retVal.Contact.Add(contact);

                    // TODO: 
                    //retVal.Link.Add(new Patient.LinkComponent()
                    //{
                    //    Other = DataTypeConverter.CreateNonVersionedReference<RelatedPerson>(rel.TargetEntityKey, restOperationContext),
                    //    Type = Patient.LinkType.Seealso
                    //});
                }
                else if (rel.RelationshipTypeKey == EntityRelationshipTypeKeys.HealthcareProvider)
                    retVal.GeneralPractitioner.Add(DataTypeConverter.CreateVersionedReference<Practitioner>(rel.LoadProperty<Entity>(nameof(EntityRelationship.TargetEntity)), restOperationContext));
                else if (rel.RelationshipTypeKey == EntityRelationshipTypeKeys.Replaces)
                    retVal.Link.Add(this.CreateLink<Practitioner>(rel, Patient.LinkType.Seealso, restOperationContext));
                else if (rel.RelationshipTypeKey == EntityRelationshipTypeKeys.Duplicate)
                    retVal.Link.Add(this.CreateLink<Patient>(rel, Patient.LinkType.Seealso, restOperationContext));
                else if (rel.RelationshipTypeKey?.ToString() == "97730a52-7e30-4dcd-94cd-fd532d111578") // MDM Master Record
                {
                    if(rel.SourceEntityKey.HasValue &&  rel.SourceEntityKey != model.Key)
                        retVal.Link.Add(this.CreateLink<Patient>(rel, Patient.LinkType.Seealso, restOperationContext));
                    else // Is a local
                        retVal.Link.Add(this.CreateLink<Patient>(rel, Patient.LinkType.Refer, restOperationContext));
                }
            }

			var photo = model.LoadCollection<EntityExtension>("Extensions").FirstOrDefault(o => o.ExtensionTypeKey == ExtensionTypeKeys.JpegPhotoExtension);
			if (photo != null)
				retVal.Photo = new List<Attachment>() {
					new Attachment()
					{
						ContentType = "image/jpg",
						Data = photo.ExtensionValueXml
					}
				};

			return retVal;
		}

        /// <summary>
        /// Create patient link
        /// </summary>
        private Patient.LinkComponent CreateLink<TLink>(EntityRelationship rel, Patient.LinkType type, RestOperationContext restOperationContext) where TLink : DomainResource, new() => new Patient.LinkComponent() 
        {
            Type = type,
            Other = DataTypeConverter.CreateNonVersionedReference<TLink>(rel.TargetEntityKey, restOperationContext)
        };

        /// <summary>
        /// Maps a FHIR patient resource to an HDSI patient.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <returns>Returns the mapped model.</returns>
        protected override Core.Model.Roles.Patient MapToModel(Patient resource, RestOperationContext restOperationContext)
		{
			var patient = new Core.Model.Roles.Patient
			{
				Addresses = resource.Address.Select(DataTypeConverter.ToEntityAddress).ToList(),
				CreationTime = DateTimeOffset.Now,
				DateOfBirthXml = resource.BirthDate,
                GenderConceptKey = DataTypeConverter.ToConcept(new Coding("http://hl7.org/fhir/administrative-gender", resource.Gender?.ToString()))?.Key,
				Identifiers = resource.Identifier.Select(DataTypeConverter.ToEntityIdentifier).ToList(),
				LanguageCommunication = resource.Communication.Select(DataTypeConverter.ToLanguageCommunication).ToList(),
				Names = resource.Name.Select(DataTypeConverter.ToEntityName).ToList(),
				Relationships = resource.Contact.Select(DataTypeConverter.ToEntityRelationship).ToList(),
				StatusConceptKey = resource.Active == null || resource.Active == true ? StatusKeys.Active : StatusKeys.Obsolete,
				Telecoms = resource.Telecom.Select(DataTypeConverter.ToEntityTelecomAddress).OfType<EntityTelecomAddress>().ToList()
			};

            patient.Extensions = resource.Extension.Select(o => DataTypeConverter.ToEntityExtension(o, patient)).ToList();
			Guid key;
			if (!Guid.TryParse(resource.Id, out key))
            {
                foreach(var id in patient.Identifiers) // try to lookup based on reliable id for the record to update
                {
                    if(id.LoadProperty<AssigningAuthority>(nameof(EntityIdentifier.Authority)).IsUnique)
                    {
                        var match = ApplicationServiceContext.Current.GetService<IRepositoryService<Core.Model.Roles.Patient>>().Find(o => o.Identifiers.Any(i => i.Authority.DomainName == id.Authority.DomainName && i.Value == id.Value), 0, 1, out int tr);
                        key = match.FirstOrDefault()?.Key ?? Guid.NewGuid();
                    }   
                }
            }
			patient.Key = key;

			if (resource.Deceased is FhirDateTime dtValue && !String.IsNullOrEmpty(dtValue.Value))
			{
				patient.DeceasedDate = dtValue.ToDateTime();
			}
			else if (resource.Deceased is FhirBoolean boolValue && boolValue.Value.GetValueOrDefault() == true)
			{
				// we don't have a field for "deceased indicator" to say that the patient is dead, but we don't know that actual date/time of death
				// should find a better way to do this
				patient.DeceasedDate = DateTime.MinValue;
				patient.DeceasedDatePrecision = DatePrecision.Year;
			}

            if (resource.MultipleBirth is FhirBoolean boolBirth && boolBirth.Value.GetValueOrDefault() == true)
            {
				patient.MultipleBirthOrder = 0;
			}
			else if (resource.MultipleBirth is Integer intBirth)
			{
				patient.MultipleBirthOrder = intBirth.Value;
			}

			return patient;
		}

        /// <summary>
        /// Map model to the resource
        /// </summary>
        /// <param name="bundleResource">The entry to be converted</param>
        /// <param name="context">The web context</param>
        /// <param name="bundle">The context for the bundle</param>
        public IdentifiedData MapToModel(Resource bundleResource, RestOperationContext context, Bundle bundle)
        {
            var patient = this.MapToModel(bundleResource as Patient, context);
            // TODO: Re-map UUIDs from the bundle uuids to the internal reference uuids.
            return patient;
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
                TypeRestfulInteraction.Delete,
                TypeRestfulInteraction.Create,
                TypeRestfulInteraction.Update
            }.Select(o => new ResourceInteractionComponent() { Code = o });
        }
    }
}