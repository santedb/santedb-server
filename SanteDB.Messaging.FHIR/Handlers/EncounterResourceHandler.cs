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
using SanteDB.Core.Model.Acts;
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
    /// Encounter resource handler for loading and disclosing of patient encounters
    /// </summary>
    public class EncounterResourceHandler : RepositoryResourceHandlerBase<Encounter, PatientEncounter>, IBundleResourceHandler
    {

        /// <summary>
        /// Map to model
        /// </summary>
        public IdentifiedData MapToModel(Resource bundleResource, RestOperationContext context, Bundle bundle)
        {
            return this.MapToModel(bundleResource as Encounter, context);
        }

        /// <summary>
        /// Get the interactions supported
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<ResourceInteractionComponent> GetInteractions()
        {
            return new List<TypeRestfulInteraction>()
            {
                TypeRestfulInteraction.SearchType,
                TypeRestfulInteraction.Read,
                TypeRestfulInteraction.Vread,
                TypeRestfulInteraction.HistoryInstance
            }.Select(o => new ResourceInteractionComponent() { Code = o });
        }

        /// <summary>
        /// Map the specified patient encounter to a FHIR based encounter
        /// </summary>
        protected override Encounter MapToFhir(PatientEncounter model, RestOperationContext restOperationContext)
        {
            var retVal = DataTypeConverter.CreateResource<Encounter>(model, restOperationContext);

            // Map the identifier
            retVal.Identifier = model.LoadCollection<ActIdentifier>("Identifiers").Select(o => DataTypeConverter.ToFhirIdentifier<Act>(o)).ToList();

            // Map status keys
            switch(model.StatusConceptKey.ToString().ToUpper())
            {
                case StatusKeyStrings.Active:
                case StatusKeyStrings.New:
                    switch(model.MoodConceptKey.ToString().ToUpper())
                    {
                        case MoodConceptKeyStrings.Eventoccurrence:
                        case MoodConceptKeyStrings.Request:
                            retVal.Status = Encounter.EncounterStatus.InProgress;
                            break;
                        case MoodConceptKeyStrings.Intent:
                        case MoodConceptKeyStrings.Promise:
                            retVal.Status = Encounter.EncounterStatus.Planned;
                            break;
                    }
                    break;
                case StatusKeyStrings.Cancelled:
                    retVal.Status = Encounter.EncounterStatus.Cancelled;
                    break;
                case StatusKeyStrings.Nullified:
                    retVal.Status = Encounter.EncounterStatus.EnteredInError;
                    break;
                    break;
                case StatusKeyStrings.Completed:
                    retVal.Status = Encounter.EncounterStatus.Finished;
                    break;
            }

            if (model.StartTime.HasValue || model.StopTime.HasValue)
                retVal.Period = DataTypeConverter.ToPeriod(model.StartTime, model.StopTime);
            else
                retVal.Period = DataTypeConverter.ToPeriod(model.ActTime, model.ActTime);

            retVal.ReasonCode = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("ReasonConcept")) };
            retVal.Type = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("TypeConcept")) };
            
            // Map associated
            var associated = model.LoadCollection<ActParticipation>("Participations");

            // Subject of encounter
            retVal.Subject = DataTypeConverter.CreateVersionedReference<Hl7.Fhir.Model.Patient>(associated.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget)?.LoadProperty<Entity>("PlayerEntity"), restOperationContext);

            // Locations
            retVal.Location = associated.Where(o => o.LoadProperty<Entity>("PlayerEntity") is Place).Select(o => new Encounter.LocationComponent()
            {
                Period = DataTypeConverter.ToPeriod(model.CreationTime, null),
                Location = DataTypeConverter.CreateVersionedReference<Location>(o.PlayerEntity, restOperationContext)
            }).ToList();

            // Service provider
            var cst = associated.FirstOrDefault(o => o.LoadProperty<Entity>("PlayerEntity") is Core.Model.Entities.Organization && o.ParticipationRoleKey == ActParticipationKey.Custodian);
            if (cst != null)
                retVal.ServiceProvider = DataTypeConverter.CreateVersionedReference<Hl7.Fhir.Model.Organization>(cst.PlayerEntity, restOperationContext);

            // Participants
            retVal.Participant = associated.Where(o => o.LoadProperty<Entity>("PlayerEntity") is Provider || o.LoadProperty<Entity>("PlayerEntity") is UserEntity).Select(o => new Encounter.ParticipantComponent()
            {
                Type = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(o.LoadProperty<Concept>("ParticipationRole")) },
                Individual = DataTypeConverter.CreateVersionedReference<Practitioner>(o.PlayerEntity, restOperationContext)
            }).ToList();


            return retVal; 
        }

        /// <summary>
        /// Map to model the encounter
        /// </summary>
        protected override PatientEncounter MapToModel(Encounter resource, RestOperationContext webOperationContext)
        {
            // Organization
            var status = resource.Status.Value;
            var retVal = new PatientEncounter()
            {
                TypeConcept = DataTypeConverter.ToConcept(resource.Class, "http://openiz.org/conceptset/v3-ActEncounterCode"),
                StartTime = resource.Period?.StartElement?.ToDateTimeOffset(),
                StopTime = resource.Period?.EndElement?.ToDateTimeOffset(),
                // TODO: Extensions
                Extensions = resource.Extension.Select(DataTypeConverter.ToActExtension).OfType<ActExtension>().ToList(),
                Identifiers = resource.Identifier.Select(DataTypeConverter.ToActIdentifier).ToList(),
                Key = Guid.NewGuid(),
                StatusConceptKey = status == Encounter.EncounterStatus.Finished ? StatusKeys.Completed :
                    status == Encounter.EncounterStatus.Cancelled ? StatusKeys.Cancelled :
                    status == Encounter.EncounterStatus.InProgress || status == Encounter.EncounterStatus.Arrived ? StatusKeys.Active :
                    status == Encounter.EncounterStatus.Planned ? StatusKeys.New : StatusKeys.Obsolete,
                MoodConceptKey = status == Encounter.EncounterStatus.Planned ? ActMoodKeys.Intent : ActMoodKeys.Eventoccurrence,
                ReasonConcept = DataTypeConverter.ToConcept(resource.ReasonCode.FirstOrDefault())
            };

            // Parse key
            Guid key;
            if (!Guid.TryParse(resource.Id, out key))
            {
                key = Guid.NewGuid();
            }
            retVal.Key = key;

            // Attempt to resolve relationships
            if (resource.Subject != null)
            {
                // Is the subject a uuid
                if (resource.Subject.Reference.StartsWith("urn:uuid:"))
                    retVal.Participations.Add(new ActParticipation(ActParticipationKey.RecordTarget, Guid.Parse(resource.Subject.Reference.Substring(9))));
                else throw new NotSupportedException("Only UUID references are supported");
            }

            // Attempt to resolve organiztaion
            if (resource.ServiceProvider != null)
            {
                // Is the subject a uuid
                if (resource.ServiceProvider.Reference.StartsWith("urn:uuid:"))
                    retVal.Participations.Add(new ActParticipation(ActParticipationKey.Custodian, Guid.Parse(resource.ServiceProvider.Reference.Substring(9))));
                else throw new NotSupportedException("Only UUID references are supported");
            }

            // TODO : Other Participations
            return retVal;
        }
    }
}
