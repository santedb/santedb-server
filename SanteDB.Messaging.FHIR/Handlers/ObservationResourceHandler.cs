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
using SanteDB.Core.Model.Query;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Observation handler
    /// </summary>
    public class ObservationResourceHandler : RepositoryResourceHandlerBase<Hl7.Fhir.Model.Observation, Core.Model.Acts.Observation>
    {
        /// <summary>
        /// Map to FHIR
        /// </summary>
        protected override Hl7.Fhir.Model.Observation MapToFhir(Core.Model.Acts.Observation model, RestOperationContext restOperationContext)
        {
            var retVal = DataTypeConverter.CreateResource<Hl7.Fhir.Model.Observation>(model, restOperationContext);
            retVal.Identifier = model.LoadCollection<ActIdentifier>(nameof(Act.Identifiers)).Select(o => DataTypeConverter.ToFhirIdentifier(o)).ToList();

            switch (model.StatusConceptKey.ToString().ToUpper())
            {
                case StatusKeyStrings.New:
                case StatusKeyStrings.Active:
                    retVal.Status = ObservationStatus.Preliminary;
                    break;
                case StatusKeyStrings.Cancelled:
                    retVal.Status = ObservationStatus.Cancelled;
                    break;
                case StatusKeyStrings.Nullified:
                    retVal.Status = ObservationStatus.EnteredInError;
                    break;
                case StatusKeyStrings.Completed:
                    if (model.LoadCollection<ActRelationship>(nameof(Act.Relationships)).Any(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.Replaces)) // was amended
                        retVal.Status = ObservationStatus.Amended;
                    else
                        retVal.Status = ObservationStatus.Final;
                    break;
                case StatusKeyStrings.Obsolete:
                    retVal.Status = ObservationStatus.Unknown;
                    break;
            }

            if (model.StartTime.HasValue || model.StopTime.HasValue)
                retVal.Effective = DataTypeConverter.ToPeriod(model.StartTime, model.StopTime);
            else
                retVal.Effective = DataTypeConverter.ToFhirDate(model.ActTime.DateTime);

            retVal.Code = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(Act.TypeConcept)));

            // RCT
            var rct = model.LoadCollection<ActParticipation>(nameof(Act.Participations)).FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget);
            if (rct != null)
            {
                retVal.Subject = DataTypeConverter.CreateNonVersionedReference<Patient>(rct.LoadProperty<Entity>(nameof(ActParticipation.PlayerEntity)), restOperationContext);
            }

            // Performer
            var prf = model.Participations.Where(o => o.ParticipationRoleKey == ActParticipationKey.Performer);
            if (prf != null)
            {
                retVal.Performer = prf.Select(o=> DataTypeConverter.CreateNonVersionedReference<Practitioner>(o.LoadProperty<Entity>(nameof(ActParticipation.PlayerEntity)), restOperationContext)).ToList();
            }

            retVal.Issued = model.CreationTime;

            // Value
            switch (model.ValueType)
            {
                case "CD":
                    retVal.Value = DataTypeConverter.ToFhirCodeableConcept((model as Core.Model.Acts.CodedObservation).Value);
                    break;
                case "PQ":
                    var qty = model as Core.Model.Acts.QuantityObservation;
                    retVal.Value = DataTypeConverter.ToQuantity(qty.Value, qty.LoadProperty<Concept>(nameof(QuantityObservation.UnitOfMeasure)));
                    break;
                case "ED":
                case "ST":
                    retVal.Value = new FhirString((model as Core.Model.Acts.TextObservation).Value);
                    break;
            }

            if (model.InterpretationConceptKey.HasValue)
                retVal.Interpretation = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(QuantityObservation.InterpretationConcept))) };

            return retVal;
        }

        /// <summary>
        /// Map to model
        /// </summary>
        protected override Core.Model.Acts.Observation MapToModel(Hl7.Fhir.Model.Observation resource, RestOperationContext restOperationContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Query
        /// </summary>
        protected override IEnumerable<Core.Model.Acts.Observation> Query(Expression<Func<Core.Model.Acts.Observation, bool>> query, Guid queryId, int offset, int count, out int totalResults)
        {
            //var anyRef = Expression.OrElse(base.CreateConceptSetFilter(ConceptSetKeys.VitalSigns, query.Parameters[0]), base.CreateConceptSetFilter(ConceptSetKeys.ProblemObservations, query.Parameters[0]));
            //query = Expression.Lambda<Func<Core.Model.Acts.Observation, bool>>(Expression.AndAlso(
            //             query.Body, 
            //             anyRef
            //         ), query.Parameters);

            return base.Query(query, queryId, offset, count, out totalResults);
        }


        /// <summary>
        /// Parameters
        /// </summary>
        public override FhirQueryResult Query(System.Collections.Specialized.NameValueCollection parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            Core.Model.Query.NameValueCollection hdsiQuery = null;
            FhirQuery query = QueryRewriter.RewriteFhirQuery<Hl7.Fhir.Model.Observation, Core.Model.Acts.Observation>(parameters, out hdsiQuery);

            // Do the query
            int totalResults = 0;

            IEnumerable<Core.Model.Acts.Observation> hdsiResults = null;

            if (parameters["value-concept"] != null)
            {
                var predicate = QueryExpressionParser.BuildLinqExpression<Core.Model.Acts.CodedObservation>(hdsiQuery);
                hdsiResults = this.QueryEx<Core.Model.Acts.CodedObservation>(predicate, query.QueryId, query.Start, query.Quantity, out totalResults).OfType<Core.Model.Acts.Observation>();
            }
            else if (parameters["value-quantity"] != null)
            {
                var predicate = QueryExpressionParser.BuildLinqExpression<Core.Model.Acts.QuantityObservation>(hdsiQuery);
                hdsiResults = this.QueryEx<Core.Model.Acts.QuantityObservation>(predicate, query.QueryId, query.Start, query.Quantity, out totalResults).OfType<Core.Model.Acts.Observation>();
            }
            else
            {
                var predicate = QueryExpressionParser.BuildLinqExpression<Core.Model.Acts.Observation>(hdsiQuery);
                hdsiResults = this.Query(predicate, query.QueryId, query.Start, query.Quantity, out totalResults);
            }


            var restOperationContext = RestOperationContext.Current;

            // Return FHIR query result
            return new FhirQueryResult()
            {
                Results = hdsiResults.Select(o => this.MapToFhir(o, restOperationContext)).OfType<Resource>().ToList(),
                Query = query,
                TotalResults = totalResults
            };
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