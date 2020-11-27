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
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.DataTypes;
using SanteDB.Messaging.FHIR.Resources;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents an immunization recommendation handler.
    /// </summary>
    public class ImmunizationRecommendationResourceHandler : ResourceHandlerBase<ImmunizationRecommendation, SubstanceAdministration>
	{
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ImmunizationRecommendationResourceHandler"/> class.
		/// </summary>
		public ImmunizationRecommendationResourceHandler()
		{
		}

		/// <summary>
		/// Creates the specified model instance.
		/// </summary>
		/// <param name="modelInstance">The model instance.</param>
		/// <param name="issues">The issues.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>Returns the created model.</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		protected override SubstanceAdministration Create(SubstanceAdministration modelInstance, TransactionMode mode)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Deletes the specified model identifier.
		/// </summary>
		/// <param name="modelId">The model identifier.</param>
		/// <param name="details">The details.</param>
		/// <returns>Returns the deleted model.</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		protected override SubstanceAdministration Delete(Guid modelId)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Maps the outbound resource to FHIR.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <returns>Returns the mapped FHIR resource.</returns>
		protected override ImmunizationRecommendation MapToFhir(SubstanceAdministration model, RestOperationContext restOperationContext)
		{
			ImmunizationRecommendation retVal = new ImmunizationRecommendation();

			retVal.Id = model.Key.ToString();
			retVal.Timestamp = DateTime.Now;
			retVal.Identifier = model.Identifiers.Select(o => DataTypeConverter.ToFhirIdentifier(o)).ToList();

			var rct = model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget).PlayerEntity;
			if (rct != null)
				retVal.Patient = Reference.CreateResourceReference(new Patient() { Id = model.Key.ToString(), VersionId = model.VersionKey.ToString() }, RestOperationContext.Current.IncomingRequest.Url);

			var mat = model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Product).PlayerEntity;

			// Recommend
			string status = (model.StopTime ?? model.ActTime) < DateTimeOffset.Now ? "overdue" : "due";
			var recommendation = new SanteDB.Messaging.FHIR.Backbone.ImmunizationRecommendation()
			{
				Date = model.CreationTime.DateTime,
				DoseNumber = model.SequenceId,
				VaccineCode = DataTypeConverter.ToFhirCodeableConcept(mat?.TypeConcept),
				ForecastStatus = new FhirCodeableConcept(new Uri("http://hl7.org/fhir/conceptset/immunization-recommendation-status"), status),
                DateCriterion = new List<SanteDB.Messaging.FHIR.Backbone.ImmunizationRecommendationDateCriterion>()
				{
					new SanteDB.Messaging.FHIR.Backbone.ImmunizationRecommendationDateCriterion()
					{
						Code = new FhirCodeableConcept(new Uri("http://hl7.org/fhir/conceptset/immunization-recommendation-date-criterion"), "recommended"),
						Value = model.ActTime.DateTime
					}
				}
			};
			if (model.StartTime.HasValue)
				recommendation.DateCriterion.Add(new SanteDB.Messaging.FHIR.Backbone.ImmunizationRecommendationDateCriterion()
				{
					Code = new FhirCodeableConcept(new Uri("http://hl7.org/fhir/conceptset/immunization-recommendation-date-criterion"), "earliest"),
					Value = model.StartTime.Value.DateTime
				});
			if (model.StopTime.HasValue)
				recommendation.DateCriterion.Add(new SanteDB.Messaging.FHIR.Backbone.ImmunizationRecommendationDateCriterion()
				{
					Code = new FhirCodeableConcept(new Uri("http://hl7.org/fhir/conceptset/immunization-recommendation-date-criterion"), "overdue"),
					Value = model.StopTime.Value.DateTime
				});

			retVal.Recommendation = new List<SanteDB.Messaging.FHIR.Backbone.ImmunizationRecommendation>() { recommendation };
			return retVal;
		}

		/// <summary>
		/// Maps a FHIR resource to a model instance.
		/// </summary>
		/// <param name="resource">The resource.</param>
		/// <returns>Returns the mapped model.</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		protected override SubstanceAdministration MapToModel(ImmunizationRecommendation resource, RestOperationContext restOperationContext)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Query for immunization recommendations.
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="issues">The issues.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The count.</param>
		/// <param name="totalResults">The total results.</param>
		/// <returns>Returns the list of models which match the given parameters.</returns>
		protected override IEnumerable<SubstanceAdministration> Query(Expression<Func<SubstanceAdministration, bool>> query, Guid queryId, int offset, int count, out int totalResults)
		{
			// TODO: Hook this up to the forecaster
			var obsoletionReference = Expression.MakeBinary(ExpressionType.NotEqual, Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(BaseEntityData.ObsoletionTime))), Expression.Constant(null));
			query = Expression.Lambda<Func<SubstanceAdministration, bool>>(Expression.AndAlso(obsoletionReference, query), query.Parameters);
            totalResults = 0;
            //return this.repository.Find<SubstanceAdministration>(query, offset, count, out totalResults);
            return null;
		}

		/// <summary>
		/// Reads the specified identifier.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="details">The details.</param>
		/// <returns>Returns the model which matches the given id.</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		protected override SubstanceAdministration Read(Guid id, Guid versionId)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Updates the specified model.
		/// </summary>
		/// <param name="model">The model.</param>
		/// <param name="details">The details.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>Returns the updated model.</returns>
		/// <exception cref="System.NotImplementedException"></exception>
		protected override SubstanceAdministration Update(SubstanceAdministration model, TransactionMode mode)
		{
			throw new NotSupportedException();
		}

        /// <summary>
        /// Get interactions
        /// </summary>
        protected override IEnumerable<SanteDB.Messaging.FHIR.Backbone.InteractionDefinition> GetInteractions()
        {
            return new SanteDB.Messaging.FHIR.Backbone.TypeRestfulInteraction[]
            {
            }.Select(o => new SanteDB.Messaging.FHIR.Backbone.InteractionDefinition() { Type = o });
        }
    }
}