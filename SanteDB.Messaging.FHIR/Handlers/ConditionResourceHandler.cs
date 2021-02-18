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
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents a handler for condition observations
    /// </summary>
    public class ConditionResourceHandler : RepositoryResourceHandlerBase<Condition, CodedObservation>
	{
		/// <summary>
		/// Map to FHIR
		/// </summary>
		protected override Condition MapToFhir(CodedObservation model, RestOperationContext restOperationContext)
		{
			var retVal = DataTypeConverter.CreateResource<Condition>(model, restOperationContext);

			retVal.Identifier = model.LoadCollection<ActIdentifier>("Identifiers").Select(o => DataTypeConverter.ToFhirIdentifier<Act>(o)).ToList();

			// Clinical status of the condition
			if (model.StatusConceptKey == StatusKeys.Active)
				retVal.ClinicalStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/condition-clinical", "active");
			else if (model.StatusConceptKey == StatusKeys.Completed)
				retVal.ClinicalStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/condition-clinical", "resolved");

			else if (model.StatusConceptKey == StatusKeys.Nullified)
				retVal.VerificationStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/condition-clinical", "entered-in-error");
			else if (model.StatusConceptKey == StatusKeys.Obsolete)
				retVal.ClinicalStatus = new CodeableConcept("http://terminology.hl7.org/CodeSystem/condition-clinical", "inactive");

			// Category
			retVal.Category.Add(new CodeableConcept("http://hl7.org/fhir/condition-category", "encounter-diagnosis"));

			// Severity?
			var actRelationshipService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ActRelationship>>();

			var severity = actRelationshipService.Query(o => o.SourceEntityKey == model.Key && o.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent && o.TargetAct.TypeConceptKey == ObservationTypeKeys.Severity, AuthenticationContext.Current.Principal);
			if (severity == null) // Perhaps we should get from neighbor if this is in an encounter
			{
				var contextAct = actRelationshipService.Query(o => o.TargetActKey == model.Key, AuthenticationContext.Current.Principal).FirstOrDefault();
				if (contextAct != null)
					severity = actRelationshipService.Query(o => o.SourceEntityKey == contextAct.SourceEntityKey && o.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent && o.TargetAct.TypeConceptKey == ObservationTypeKeys.Severity, AuthenticationContext.Current.Principal);
			}

			// Severity
			if (severity != null)
				retVal.Severity = DataTypeConverter.ToFhirCodeableConcept((severity as CodedObservation).LoadProperty<Concept>("Value"));

			retVal.Code = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("Value"));

			// body sites?
			var sites = actRelationshipService.Query(o => o.SourceEntityKey == model.Key && o.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent && o.TargetAct.TypeConceptKey == ObservationTypeKeys.FindingSite, AuthenticationContext.Current.Principal);
			retVal.BodySite = sites.Select(o => DataTypeConverter.ToFhirCodeableConcept(o.LoadProperty<CodedObservation>("TargetAct").LoadProperty<Concept>("Value"))).ToList();

			// Subject
			var recordTarget = model.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget);
            if (recordTarget != null)
            {
                this.traceSource.TraceInfo("RCT: {0}", recordTarget.PlayerEntityKey);
                retVal.Subject = DataTypeConverter.CreateVersionedReference<Patient>(recordTarget.LoadProperty<Entity>("PlayerEntity"), restOperationContext);
            }
			// Onset
			if (model.StartTime.HasValue || model.StopTime.HasValue)
				retVal.Onset = new Period()
				{
					StartElement = model.StartTime.HasValue ? new FhirDateTime(model.StartTime.Value) : null,
					EndElement = model.StopTime.HasValue ? new FhirDateTime(model.StopTime.Value) : null
				};
			else
				retVal.Onset = new FhirDateTime(model.ActTime);

			retVal.RecordedDateElement = new FhirDateTime(model.CreationTime);
			var author = model.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Authororiginator);
			if (author != null)
				retVal.Asserter = DataTypeConverter.CreateNonVersionedReference<Practitioner>(author.LoadProperty<Entity>("PlayerEntity"), restOperationContext);

			return retVal;
		}

        /// <summary>
        /// Maps a FHIR condition to a model
        /// </summary>
        /// <param name="resource">The FHIR condition to be mapped</param>
        /// <param name="restOperationContext">The REST operation context under which this method is called</param>
        /// <returns>The constructed model instance</returns>
		protected override CodedObservation MapToModel(Condition resource, RestOperationContext restOperationContext)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Query filter
		/// </summary>
		protected override IEnumerable<CodedObservation> Query(Expression<Func<CodedObservation, bool>> query, Guid queryId, int offset, int count, out int totalResults)
		{
			var anyRef = base.CreateConceptSetFilter(ConceptSetKeys.ProblemObservations, query.Parameters[0]);
			query = System.Linq.Expressions.Expression.Lambda<Func<CodedObservation, bool>>(System.Linq.Expressions.Expression.AndAlso(query.Body, anyRef), query.Parameters);

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
            }.Select(o => new ResourceInteractionComponent() { Code = o });
        }
    }
}