/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using RestSrvr;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
			query = Expression.Lambda<Func<CodedObservation, bool>>(Expression.AndAlso(query.Body, anyRef), query.Parameters);
			return base.Query(query, queryId, offset, count, out totalResults);
		}

        /// <summary>
        /// Get interactions
        /// </summary>
        protected override IEnumerable<InteractionDefinition> GetInteractions()
        {
            return new TypeRestfulInteraction[]
            {
            }.Select(o => new InteractionDefinition() { Type = o });
        }
    }
}