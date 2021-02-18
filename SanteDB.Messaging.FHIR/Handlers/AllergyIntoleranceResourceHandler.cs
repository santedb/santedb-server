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
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static Hl7.Fhir.Model.CapabilityStatement;

namespace SanteDB.Messaging.FHIR.Handlers
{
    /// <summary>
    /// Allergy / intolerance resource handler
    /// </summary>
    public class AllergyIntoleranceResourceHandler : RepositoryResourceHandlerBase<AllergyIntolerance, CodedObservation>
	{
        /// <summary>
        /// Map coded allergy intolerance resource to FHIR
        /// </summary>
		protected override AllergyIntolerance MapToFhir(CodedObservation model, RestOperationContext restOperationContext)
		{
			throw new NotImplementedException();
		}

        /// <summary>
        /// Map allergy intolerance from FHIR to a coded observation
        /// </summary>
		protected override CodedObservation MapToModel(AllergyIntolerance resource, RestOperationContext restOperationContext)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Query which filters only allergies and intolerances
		/// </summary>
		protected override IEnumerable<CodedObservation> Query(Expression<Func<CodedObservation, bool>> query, Guid queryId, int offset, int count, out int totalResults)
		{
			var anyRef = base.CreateConceptSetFilter(ConceptSetKeys.AllergyIntoleranceTypes, query.Parameters[0]);
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
            }.Select(o => new ResourceInteractionComponent() { Code = o });
        }
    }
}