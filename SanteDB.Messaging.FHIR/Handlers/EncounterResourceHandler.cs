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
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
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
    /// Encounter resource handler for loading and disclosing of patient encounters
    /// </summary>
    public class EncounterResourceHandler : RepositoryResourceHandlerBase<Encounter, PatientEncounter>, IBundleResourceHandler
    {

        /// <summary>
        /// Map to model
        /// </summary>
        public IdentifiedData MapToModel(BundleEntry bundleResource, RestOperationContext context, Bundle bundle)
        {
            return this.MapToModel(bundleResource.Resource.Resource as Encounter, context);
        }

        /// <summary>
        /// Get the interactions supported
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<InteractionDefinition> GetInteractions()
        {
            return new List<TypeRestfulInteraction>()
            {
                TypeRestfulInteraction.Search,
                TypeRestfulInteraction.Read,
                TypeRestfulInteraction.VersionRead,
                TypeRestfulInteraction.InstanceHistory
            }.Select(o => new InteractionDefinition() { Type = o });
        }

        /// <summary>
        /// Map the specified patient encounter to a FHIR based encounter
        /// </summary>
        protected override Encounter MapToFhir(PatientEncounter model, RestOperationContext restOperationContext)
        {
            var retVal = DataTypeConverter.CreateResource<Encounter>(model);

            // Map the identifier
            retVal.Identifier = model.LoadCollection<ActIdentifier>("Identifiers").Select(o => DataTypeConverter.ToFhirIdentifier<Act>(o)).ToList();

            // Map status keys
            switch(model.StatusConceptKey.ToString().ToUpper())
            {
                case StatusKeyStrings.Active:
                    retVal.Status = EncounterStatus.InProgress;
                    break;
                case StatusKeyStrings.Cancelled:
                case StatusKeyStrings.Nullified:
                    retVal.Status = EncounterStatus.Cancelled;
                    break;
                case StatusKeyStrings.Completed:
                    retVal.Status = EncounterStatus.Finished;
                    break;
            }

            if (model.StartTime.HasValue || model.StopTime.HasValue)
                retVal.Period = new FhirPeriod()
                {
                    Start = model.StartTime?.DateTime,
                    Stop = model.StopTime?.DateTime
                };
            else
                retVal.Period = new FhirPeriod()
                {
                    Start = model.ActTime.DateTime,
                    Stop = model.ActTime.DateTime
                };

            retVal.Reason = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("ReasonConcept"));
            retVal.Type = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("TypeConcept"));
            
            // Map associated
            var associated = model.LoadCollection<ActParticipation>("Participations");

            // Subject of encounter
            retVal.Subject = DataTypeConverter.CreateReference<SanteDB.Messaging.FHIR.Resources.Patient>(associated.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget)?.LoadProperty<Entity>("PlayerEntity"), restOperationContext);

            // Locations
            retVal.Location = associated.Where(o => o.LoadProperty<Entity>("PlayerEntity") is Place).Select(o => new EncounterLocation()
            {
                Period = new FhirPeriod() { Start = model.CreationTime.DateTime },
                Location = DataTypeConverter.CreateReference<Location>(o.PlayerEntity, restOperationContext)
            }).ToList();

            // Service provider
            var cst = associated.FirstOrDefault(o => o.LoadProperty<Entity>("PlayerEntity") is Core.Model.Entities.Organization && o.ParticipationRoleKey == ActParticipationKey.Custodian);
            if (cst != null)
                retVal.ServiceProvider = DataTypeConverter.CreateReference<SanteDB.Messaging.FHIR.Resources.Organization>(cst.PlayerEntity, restOperationContext);

            // Participants
            retVal.Participant = associated.Where(o => o.LoadProperty<Entity>("PlayerEntity") is Provider || o.LoadProperty<Entity>("PlayerEntity") is UserEntity).Select(o => new EncounterParticipant()
            {
                Period = new FhirPeriod() { Start = model.CreationTime.DateTime },
                Type = new List<FhirCodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(o.LoadProperty<Concept>("ParticipationRole")) },
                Individual = DataTypeConverter.CreateReference<Practitioner>(o.PlayerEntity, restOperationContext)
            }).ToList();


            return retVal; 
        }

        /// <summary>
        /// Map to model the encounter
        /// </summary>
        protected override PatientEncounter MapToModel(Encounter resource, RestOperationContext webOperationContext)
        {
            // Organization
            var status = resource.Status?.Value;
            var retVal = new PatientEncounter()
            {
                TypeConcept = DataTypeConverter.ToConcept(resource.Class, new Uri("http://openiz.org/conceptset/v3-ActEncounterCode")),
                StartTime = resource.Period?.Start?.DateValue,
                StopTime = resource.Period?.Stop?.DateValue,
                // TODO: Extensions
                Extensions = resource.Extension.Select(DataTypeConverter.ToActExtension).OfType<ActExtension>().ToList(),
                Identifiers = resource.Identifier.Select(DataTypeConverter.ToActIdentifier).ToList(),
                Key = Guid.NewGuid(),
                StatusConceptKey = status == EncounterStatus.Finished ? StatusKeys.Completed :
                    status == EncounterStatus.Cancelled ? StatusKeys.Cancelled :
                    status == EncounterStatus.InProgress || status == EncounterStatus.Arrived ? StatusKeys.Active :
                    status == EncounterStatus.Planned ? StatusKeys.Active : StatusKeys.Obsolete,
                MoodConceptKey = status == EncounterStatus.Planned ? ActMoodKeys.Intent : ActMoodKeys.Eventoccurrence,
                ReasonConcept = DataTypeConverter.ToConcept(resource.Reason)
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
                if (resource.Subject.ReferenceUrl.Value.StartsWith("urn:uuid:"))
                    retVal.Participations.Add(new ActParticipation(ActParticipationKey.RecordTarget, Guid.Parse(resource.Subject.ReferenceUrl.Value.Substring(9))));
                else throw new NotSupportedException("Only UUID references are supported");
            }

            // Attempt to resolve organiztaion
            if (resource.ServiceProvider != null)
            {
                // Is the subject a uuid
                if (resource.ServiceProvider.ReferenceUrl.Value.StartsWith("urn:uuid:"))
                    retVal.Participations.Add(new ActParticipation(ActParticipationKey.Custodian, Guid.Parse(resource.ServiceProvider.ReferenceUrl.Value.Substring(9))));
                else throw new NotSupportedException("Only UUID references are supported");
            }

            // TODO : Other Participations
            return retVal;
        }
    }
}
