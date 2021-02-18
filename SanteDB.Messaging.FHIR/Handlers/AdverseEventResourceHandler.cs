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
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{

    /// <summary>
    /// Adverse event resource handler
    /// </summary>
    public class AdverseEventResourceHandler : RepositoryResourceHandlerBase<AdverseEvent, Act>
    {
        /// <summary>
        /// Maps the specified act to an adverse event
        /// </summary>
        protected override AdverseEvent MapToFhir(Act model, RestOperationContext restOperationContext)
        {
            var retVal = DataTypeConverter.CreateResource<AdverseEvent>(model, restOperationContext);

            retVal.Identifier = DataTypeConverter.ToFhirIdentifier<Act>(model.Identifiers.FirstOrDefault());
            retVal.Category = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("TypeConcept")) };

            var recordTarget = model.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget);
            if (recordTarget != null)
                retVal.Subject = DataTypeConverter.CreateVersionedReference<Patient>(recordTarget.LoadProperty<Entity>("PlayerEntity"), restOperationContext);

            // Main topic of the concern
            var subject = model.LoadCollection<ActRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.HasSubject)?.LoadProperty<Act>("TargetAct");
            if (subject == null) throw new InvalidOperationException("This act does not appear to be an adverse event");
            retVal.DateElement = new FhirDateTime(subject.ActTime.DateTime);

            // Reactions = HasManifestation
            var reactions = subject.LoadCollection<ActRelationship>("Relationships").Where(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.HasManifestation).FirstOrDefault();
            if (reactions != null)
            {
                retVal.Event = DataTypeConverter.ToFhirCodeableConcept(reactions.LoadProperty<CodedObservation>("TargetAct").LoadProperty<Concept>(nameof(CodedObservation.Value)));
            }

            var location = model.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Location);
            if (location != null)
            {
                retVal.Location = DataTypeConverter.CreateVersionedReference<Location>(location.LoadProperty<Entity>("PlayerEntity"), restOperationContext);
            }

            // Severity
            var severity = subject.LoadCollection<ActRelationship>("Relationships").First(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent && o.LoadProperty<Act>("TargetAct").TypeConceptKey == ObservationTypeKeys.Severity);
            if (severity != null)
                retVal.Seriousness = DataTypeConverter.ToFhirCodeableConcept(severity.LoadProperty<CodedObservation>("TargetAct").Value, "http://hl7.org/fhir/adverse-event-seriousness");

            // Did the patient die?
            var causeOfDeath = model.LoadCollection<ActRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.IsCauseOf && o.LoadProperty<Act>("TargetAct").TypeConceptKey == ObservationTypeKeys.ClinicalState && (o.TargetAct as CodedObservation)?.ValueKey == Guid.Parse("6df3720b-857f-4ba2-826f-b7f1d3c3adbb"));
            if (causeOfDeath != null)
                retVal.Outcome = new CodeableConcept("http://hl7.org/fhir/adverse-event-outcome", "fatal");
            else if (model.StatusConceptKey == StatusKeys.Active)
                retVal.Outcome = new CodeableConcept("http://hl7.org/fhir/adverse-event-outcome", "ongoing");
            else if (model.StatusConceptKey == StatusKeys.Completed)
                retVal.Outcome = new CodeableConcept("http://hl7.org/fhir/adverse-event-outcome", "resolved");

            var author = model.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Authororiginator);
            if (author != null)
                retVal.Recorder = DataTypeConverter.CreateNonVersionedReference<Practitioner>(author.LoadProperty<Entity>("PlayerEntity"), restOperationContext);

            // Suspect entities
            var refersTo = model.LoadCollection<ActRelationship>("Relationships").Where(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.RefersTo);
            if (refersTo.Count() > 0)
                retVal.SuspectEntity = refersTo.Select(o => o.LoadProperty<Act>("TargetAct")).OfType<SubstanceAdministration>().Select(o =>
                {
                    var consumable = o.LoadCollection<ActParticipation>("Participations").FirstOrDefault(x => x.ParticipationRoleKey == ActParticipationKey.Consumable)?.LoadProperty<ManufacturedMaterial>("PlayerEntity");
                    if (consumable == null)
                    {
                        var product = o.LoadCollection<ActParticipation>("Participations").FirstOrDefault(x => x.ParticipationRoleKey == ActParticipationKey.Product)?.LoadProperty<Material>("PlayerEntity");
                        return new AdverseEvent.SuspectEntityComponent() { Instance = DataTypeConverter.CreateNonVersionedReference<Substance>(product, restOperationContext) };
                    }
                    else
                        return new AdverseEvent.SuspectEntityComponent() { Instance = DataTypeConverter.CreateNonVersionedReference<Medication>(consumable, restOperationContext) };
                }).ToList();

            return retVal;
        }

        /// <summary>
        /// Map adverse events to the model 
        /// </summary>
        protected override Act MapToModel(AdverseEvent resource, RestOperationContext restOperationContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query for specified adverse event 
        /// </summary>
        protected override IEnumerable<Act> Query(Expression<Func<Act, bool>> query, Guid queryId, int offset, int count, out int totalResults)
        {
            var typeReference = System.Linq.Expressions.Expression.MakeBinary(ExpressionType.Equal, System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.MakeMemberAccess(query.Parameters[0], typeof(Act).GetProperty(nameof(Act.ClassConceptKey))), typeof(Guid)), System.Linq.Expressions.Expression.Constant(ActClassKeys.Condition));

            var anyRef = base.CreateConceptSetFilter(ConceptSetKeys.AdverseEventActs, query.Parameters[0]);
            query = System.Linq.Expressions.Expression.Lambda<Func<Act, bool>>(System.Linq.Expressions.Expression.AndAlso(System.Linq.Expressions.Expression.AndAlso(query.Body, anyRef), typeReference), query.Parameters);

            return base.Query(query, queryId, offset, count, out totalResults);
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
            }.Select(o=> new ResourceInteractionComponent() { Code = o });
        }
    }
}