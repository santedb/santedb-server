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

using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Represents a resource handler for medication administration resources
    /// </summary>
    public class MedicationAdministrationResourceHandler : RepositoryResourceHandlerBase<MedicationAdministration, SubstanceAdministration>
    {
        /// <summary>
        /// Maps the object to model to fhir
        /// </summary>
        protected override MedicationAdministration MapToFhir(SubstanceAdministration model, RestOperationContext restOperationContext)
        {
            var retVal = DataTypeConverter.CreateResource<MedicationAdministration>(model, restOperationContext);

            retVal.Identifier = model.LoadCollection<ActIdentifier>(nameof(Act.Identifiers)).Select(o => DataTypeConverter.ToFhirIdentifier(o)).ToList();
            retVal.StatusReason = new List<CodeableConcept>() { DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(Act.ReasonConcept))) };
            switch(model.StatusConceptKey.ToString().ToUpper())
            {
                case StatusKeyStrings.Active:
                    retVal.Status = MedicationAdministration.MedicationAdministrationStatusCodes.InProgress;
                    break;
                case StatusKeyStrings.Cancelled:
                    retVal.Status = MedicationAdministration.MedicationAdministrationStatusCodes.Stopped;
                    break;
                case StatusKeyStrings.Nullified:
                    retVal.Status = MedicationAdministration.MedicationAdministrationStatusCodes.EnteredInError;
                    break;
                case StatusKeyStrings.Completed:
                    retVal.Status = MedicationAdministration.MedicationAdministrationStatusCodes.Completed;
                    break;
                case StatusKeyStrings.Obsolete:
                    retVal.Status = MedicationAdministration.MedicationAdministrationStatusCodes.Unknown;
                    break;
            }

            if (model.IsNegated)
                retVal.Status = MedicationAdministration.MedicationAdministrationStatusCodes.NotDone;

            retVal.Category = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(Entity.TypeConcept)), "http://hl7.org/fhir/medication-admin-category");

            var consumableRelationship = model.LoadCollection<ActParticipation>(nameof(Act.Participations)).FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Consumable);
            var productRelationship = model.LoadCollection<ActParticipation>(nameof(Act.Participations)).FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Product);
            if (consumableRelationship != null)
                retVal.Medication = DataTypeConverter.CreateVersionedReference<Medication>(consumableRelationship.LoadProperty<ManufacturedMaterial>("PlayerEntity"), restOperationContext);
            else if (productRelationship != null)
            {
                retVal.Medication = DataTypeConverter.CreateVersionedReference<Substance>(productRelationship.LoadProperty<Material>("PlayerEntity"), restOperationContext);
                //retVal.Medication = DataTypeConverter.ToFhirCodeableConcept(productRelationship.LoadProperty<Material>("PlayerEntity").LoadProperty<Concept>("TypeConcept"));
            }

            var rct = model.LoadCollection<ActParticipation>(nameof(Act.Participations)).FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.RecordTarget);
            if (rct != null)
                retVal.Subject = DataTypeConverter.CreateVersionedReference<Patient>(rct.LoadProperty<Entity>("PlayerEntity"), restOperationContext);

            // Encounter
            var erService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>();
            int tr = 0;
            var enc = erService.Query(o => o.TargetEntityKey == model.Key && o.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent && o.ObsoleteVersionSequenceId == null, 0, 10,  out tr, AuthenticationContext.Current.Principal);
            if (enc != null)
            {
                retVal.EventHistory = enc.Select(o => DataTypeConverter.CreateNonVersionedReference<Encounter>(o.TargetEntityKey, restOperationContext)).ToList();
                // TODO: Encounter
            }

            // Effective time
            retVal.Effective = DataTypeConverter.ToPeriod(model.StartTime ?? model.ActTime, model.StopTime);

            // performer
            var performer = model.LoadCollection<ActParticipation>(nameof(Act.Participations)).Where(o => o.ParticipationRoleKey == ActParticipationKey.Performer || o.ParticipationRoleKey == ActParticipationKey.Authororiginator);
            if (performer != null)
                retVal.Performer = performer.Select(o => new MedicationAdministration.PerformerComponent()
                {
                    Actor = DataTypeConverter.CreateVersionedReference<Practitioner>(o.LoadProperty<Entity>(nameof(ActParticipation.PlayerEntity)), restOperationContext)
                }).ToList();


            retVal.Dosage = new MedicationAdministration.DosageComponent()
            {
                Site = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("Site")),
                Route = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>("Route")),
                Dose = new SimpleQuantity()
                {
                    Value = model.DoseQuantity,
                    Unit = DataTypeConverter.ToFhirCodeableConcept(model.LoadProperty<Concept>(nameof(SubstanceAdministration.DoseUnit)), "http://hl7.org/fhir/sid/ucum").GetCoding()?.Code
                }
            };

            return retVal;
        }

        /// <summary>
        /// Map from FHIR to model
        /// </summary>
        protected override SubstanceAdministration MapToModel(MedicationAdministration resource, RestOperationContext restOperationContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
		/// Query for substance administrations that aren't immunizations
		/// </summary>
		/// <param name="query">The query.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The count.</param>
		/// <param name="totalResults">The total results.</param>
        /// <param name="queryId">The unique query state identifier</param>
		/// <returns>Returns the list of models which match the given parameters.</returns>
		protected override IEnumerable<SubstanceAdministration> Query(System.Linq.Expressions.Expression<Func<SubstanceAdministration, bool>> query, Guid queryId, int offset, int count, out int totalResults)
        {
            Guid drugTherapy = Guid.Parse("7D84A057-1FCC-4054-A51F-B77D230FC6D1");

            var obsoletionReference = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.StatusConceptKey))), typeof(Guid)), System.Linq.Expressions.Expression.Constant(StatusKeys.Completed));
            var typeReference = System.Linq.Expressions.Expression.MakeBinary(System.Linq.Expressions.ExpressionType.Equal, System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.MakeMemberAccess(query.Parameters[0], typeof(SubstanceAdministration).GetProperty(nameof(SubstanceAdministration.TypeConceptKey))), typeof(Guid)), System.Linq.Expressions.Expression.Constant(drugTherapy));

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
                TypeRestfulInteraction.Delete
            }.Select(o => new ResourceInteractionComponent() { Code = o });
        }
    }
}
