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
using SanteDB.Core.Services;
using SanteDB.Messaging.FHIR.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Resource handler for immunization classes.
    /// </summary>
    public class ImmunizationResourceHandler : RepositoryResourceHandlerBase<Immunization, SubstanceAdministration>, IBundleResourceHandler
	{
		/// <summary>
		/// Maps the substance administration to FHIR.
		/// </summary>
		/// <param name="model">The model.</param>
        /// <param name="restOperationContext">The operation context in which this method is being called</param>
		/// <returns>Returns the mapped FHIR resource.</returns>
		protected override Immunization MapToFhir(SubstanceAdministration model, RestOperationContext restOperationContext)
		{
			var retVal = DataTypeConverter.CreateResource<Immunization>(model, restOperationContext);

            retVal.DoseQuantity = new SimpleQuantity()
            {
                Unit = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(SubstanceAdministration.DoseUnit)), "http://hl7.org/fhir/sid/ucum")?.GetCoding().Code,
				Value = model.DoseQuantity
			};
            retVal.RecordedElement = new FhirDateTime(model.ActTime); // TODO: This is probably not the best place to put this?
			retVal.Route = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(SubstanceAdministration.Route)));
			retVal.Site = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(SubstanceAdministration.Site)));
            retVal.StatusReason = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(SubstanceAdministration.ReasonConcept)));
            switch(model.StatusConceptKey?.ToString().ToUpper())
            {
                case StatusKeyStrings.Completed:
                    if (model.IsNegated)
                        retVal.Status = Immunization.ImmunizationStatusCodes.NotDone;
                    else 
                        retVal.Status = Immunization.ImmunizationStatusCodes.Completed;
                    break;
                case StatusKeyStrings.Nullified:
                    retVal.Status = Immunization.ImmunizationStatusCodes.EnteredInError;
                    break;
            }

			// Material
			var matPtcpt = model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Consumable) ??
				model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Product);
			if (matPtcpt != null)
			{
				var matl = matPtcpt.LoadProperty<Material>(nameof(ActParticipation.PlayerEntity));
				retVal.VaccineCode = DataTypeConverter.ToFhirCodeableConcept(matl.LoadProperty<Concept>(nameof(Act.TypeConcept)));
				retVal.ExpirationDateElement = matl.ExpiryDate.HasValue ? DataTypeConverter.ToFhirDate(matl.ExpiryDate) : null;
				retVal.LotNumber = (matl as ManufacturedMaterial)?.LotNumber;
			}
			else
				retVal.ExpirationDate = null;

			// RCT
			var rct = model.Participations.FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget);
			if (rct != null)
			{
				retVal.Patient = DataTypeConverter.CreateVersionedReference<Patient>(rct.LoadProperty<Entity>("PlayerEntity"), restOperationContext);
			}

			// Performer
			var prf = model.Participations.Where(o => o.ParticipationRoleKey == ActParticipationKey.Performer || o.ParticipationRoleKey == ActParticipationKey.Authororiginator);
            if (prf != null)
                retVal.Performer = prf.Select(o =>
                    new Immunization.PerformerComponent()
                    {
                        Actor = DataTypeConverter.CreateVersionedReference<Practitioner>(o.LoadProperty<Entity>(nameof(ActParticipation.PlayerEntity)), restOperationContext)
                    }).ToList();

			// Protocol
			foreach (var itm in model.Protocols)
			{
				Immunization.ProtocolAppliedComponent protocol = new Immunization.ProtocolAppliedComponent();
				var dbProtocol = itm.LoadProperty<Protocol>(nameof(ActProtocol.Protocol));
				protocol.DoseNumber = new Integer(model.SequenceId);

				// Protocol lookup
				protocol.Series = dbProtocol?.Name;
				retVal.ProtocolApplied.Add(protocol);
			}



			return retVal;
		}

		/// <summary>
		/// Map an immunization FHIR resource to a substance administration.
		/// </summary>
		/// <param name="resource">The resource.</param>
        /// <param name="restOperationContext">The operation context in which this method is being called</param>
		/// <returns>Returns the mapped model.</returns>
		protected override SubstanceAdministration MapToModel(Immunization resource, RestOperationContext restOperationContext)
		{
            var substanceAdministration = new SubstanceAdministration
            {
                ActTime = resource.RecordedElement.ToDateTimeOffset(),
                DoseQuantity = resource.DoseQuantity?.Value.Value ?? 0,
                DoseUnit = resource.DoseQuantity != null ? DataTypeConverter.ToConcept<String>(resource.DoseQuantity.Unit, "http://hl7.org/fhir/sid/ucum") : null,
                Extensions = resource.Extension?.Select(DataTypeConverter.ToActExtension).ToList(),
                Identifiers = resource.Identifier?.Select(DataTypeConverter.ToActIdentifier).ToList(),
                Key = Guid.NewGuid(),
                MoodConceptKey = ActMoodKeys.Eventoccurrence,
                StatusConceptKey = resource.Status == Immunization.ImmunizationStatusCodes.Completed ? StatusKeys.Completed : resource.Status == Immunization.ImmunizationStatusCodes.EnteredInError ? StatusKeys.Nullified : StatusKeys.Completed,
                IsNegated = resource.Status == Immunization.ImmunizationStatusCodes.NotDone,
                RouteKey = DataTypeConverter.ToConcept(resource.Route)?.Key,
                SiteKey = DataTypeConverter.ToConcept(resource.Site)?.Key,
            };


            Guid key;
            if (Guid.TryParse(resource.Id, out key))
            {
                substanceAdministration.Key = key;
            }

            // Patient
            if (resource.Patient != null)
            {
                // Is the subject a uuid
                if (resource.Patient.Reference.StartsWith("urn:uuid:"))
                    substanceAdministration.Participations.Add(new ActParticipation(ActParticipationKey.RecordTarget, Guid.Parse(resource.Patient.Reference.Substring(9))));
                else throw new NotSupportedException("Only UUID references are supported");
            }

            // Encounter
            if (resource.Encounter != null)
            {
                // Is the subject a uuid
                if (resource.Encounter.Reference.StartsWith("urn:uuid:"))
                    substanceAdministration.Relationships.Add(new ActRelationship(ActRelationshipTypeKeys.HasComponent, substanceAdministration.Key)
                    {
                        SourceEntityKey = Guid.Parse(resource.Encounter.Reference.Substring(9))
                    });
                else throw new NotSupportedException("Only UUID references are supported");
            }

            // Find the material that was issued
            if (resource.VaccineCode != null)
            {
                var concept = DataTypeConverter.ToConcept(resource.VaccineCode);
                if (concept == null)
                {
                    this.traceSource.TraceWarning("Ignoring administration {0} don't have concept mapped", resource.VaccineCode);
                    return null;
                }
                // Get the material 
                int t = 0;
                var material = ApplicationServiceContext.Current.GetService<IRepositoryService<Material>>().Find(m => m.TypeConceptKey == concept.Key, 0, 1, out t).FirstOrDefault();
                if (material == null)
                {
                    this.traceSource.TraceWarning("Ignoring administration {0} don't have material registered for {1}", resource.VaccineCode, concept?.Mnemonic);
                    return null;
                }
                else
                {
                    substanceAdministration.Participations.Add(new ActParticipation(ActParticipationKey.Product, material.Key));
                    if (resource.LotNumber != null)
                    {
                        // TODO: Need to also find where the GTIN is kept
                        var mmaterial = ApplicationServiceContext.Current.GetService<IRepositoryService<ManufacturedMaterial>>().Find(o => o.LotNumber == resource.LotNumber && o.Relationships.Any(r => r.SourceEntityKey == material.Key && r.RelationshipTypeKey == EntityRelationshipTypeKeys.Instance));
                        substanceAdministration.Participations.Add(new ActParticipation(ActParticipationKey.Consumable, material.Key) { Quantity = 1 });
                    }

                    // Get dose units
                    if (substanceAdministration.DoseQuantity == 0)
                    {
                        substanceAdministration.DoseQuantity = 1;
                        substanceAdministration.DoseUnitKey = material.QuantityConceptKey;
                    }
                }
            }

            return substanceAdministration;
        }

		/// <summary>
		/// Query for substance administrations.
		/// </summary>
		/// <param name="query">The query to be executed</param>
		/// <param name="offset">The offset to the first result</param>
		/// <param name="count">The count of results in the current result set</param>
		/// <param name="totalResults">The total results</param>
        /// <param name="queryId">The unique query state identifier</param>
		/// <returns>Returns the list of models which match the given parameters.</returns>
		protected override IEnumerable<SubstanceAdministration> Query(System.Linq.Expressions.Expression<Func<SubstanceAdministration, bool>> query, Guid queryId, int offset, int count, out int totalResults)
		{
			Guid initialImmunization = Guid.Parse("f3be6b88-bc8f-4263-a779-86f21ea10a47"),
				immunization = Guid.Parse("6e7a3521-2967-4c0a-80ec-6c5c197b2178"),
				boosterImmunization = Guid.Parse("0331e13f-f471-4fbd-92dc-66e0a46239d5");

			var obsoletionReference = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.StatusConceptKey))), typeof(Guid)), System.Linq.Expressions.Expression.Constant(StatusKeys.Completed));
			var typeReference = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Or,
				System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Or,
					System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.TypeConceptKey))), typeof(Guid)), System.Linq.Expressions.Expression.Constant(initialImmunization)),
					System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.TypeConceptKey))), typeof(Guid)), System.Linq.Expressions.Expression.Constant(immunization))
				),
				System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.TypeConceptKey))), typeof(Guid)), System.Linq.Expressions.Expression.Constant(boosterImmunization))
			);

			query = System.Linq.Expressions.Expression.Lambda<Func<SubstanceAdministration, bool>>(System.Linq.Expressions.Expression.AndAlso(System.Linq.Expressions.Expression.AndAlso(obsoletionReference, query.Body), typeReference), query.Parameters);

			if (queryId == Guid.Empty)
				return this.m_repository.Find(query, offset, count, out totalResults);
			else
				return (this.m_repository as IPersistableQueryRepositoryService<SubstanceAdministration>).Find(query, offset, count, out totalResults, queryId);
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
                TypeRestfulInteraction.Create,
                TypeRestfulInteraction.Update,
                TypeRestfulInteraction.Delete
            }.Select(o => new ResourceInteractionComponent() { Code = o });
        }

        /// <summary>
        /// Map to model
        /// </summary>
        public IdentifiedData MapToModel(Resource bundleResource, RestOperationContext context, Bundle bundle)
        {
            return this.MapToModel(bundleResource as Immunization, context);
        }
    }
}