/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2017-9-1
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Roles;
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Services;
using System.ServiceModel.Web;
using SanteDB.Messaging.HDSI.Util;
using SanteDB.Core.Model.Acts;
using System.Linq.Expressions;
using SanteDB.Core.Security;
using SanteDB.Core.Interop;
using SanteDB.Messaging.Common;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
    /// <summary>
    /// Represents a care plan resource handler
    /// </summary>
    public class CareplanResourceHandler : IResourceHandler
    {
        /// <summary>
        /// Get capabilities statement
        /// </summary>
        public ResourceCapability Capabilities
        {
            get
            {
                return ResourceCapability.Get | ResourceCapability.Search;
            }
        }
        
        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(Wcf.IHdsiServiceContract);

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName
        {
            get
            {
                return "CarePlan";
            }
        }

        /// <summary>
        /// Gets the type that this produces
        /// </summary>
        public Type Type
        {
            get
            {
                return typeof(CarePlan);
            }
        }

        /// <summary>
        /// Create a care plan
        /// </summary>
        public Object Create(Object data, bool updateIfExists)
        {

            (data as Bundle)?.Reconstitute();
            data = (data as CarePlan)?.Target ?? data as Patient;
            if (data == null)
                throw new InvalidOperationException("Careplan requires a patient or bundle containing a patient entry");

            // Get care plan service
            var carePlanner = ApplicationContext.Current.GetService<ICarePlanService>();
            var plan = carePlanner.CreateCarePlan(data as Patient, 
                WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["_asEncounters"] == "true",
                WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters.ToQuery().ToDictionary(o=>o.Key, o=>(Object)o.Value));

            // Expand the participation roles form the care planner
            IConceptRepositoryService conceptService = ApplicationContext.Current.GetService<IConceptRepositoryService>();
            foreach (var p in plan.Action)
                p.Participations.ForEach(o => o.ParticipationRoleKey = o.ParticipationRoleKey ?? conceptService.GetConcept(o.ParticipationRole?.Mnemonic).Key);
            return Bundle.CreateBundle(plan);

        }

        /// <summary>
        /// Gets a careplan by identifier
        /// </summary>
        public Object Get(object id, object versionId)
        {
            // TODO: This will become the retrieval of care plan object 
            throw new NotSupportedException();
        }

        /// <summary>
        /// Obsolete the care plan
        /// </summary>
        public Object Obsolete(object  key)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Query for care plan
        /// </summary>
        public IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Query for care plan objects... Constructs a care plan for all patients matching the specified query parameters
        /// </summary>
        public IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var repositoryService = ApplicationContext.Current.GetService<IRepositoryService<Patient>>();
            if (repositoryService == null)
                throw new InvalidOperationException("Could not find patient repository service");
            
            // Query
            var carePlanner = ApplicationContext.Current.GetService<ICarePlanService>();

            Expression<Func<Patient, bool>> queryExpr = QueryExpressionParser.BuildLinqExpression<Patient>(queryParameters);
            List<String> queryId = null;
            IEnumerable<Patient> patients = null;
            if (queryParameters.TryGetValue("_queryId", out queryId) && repositoryService is IPersistableQueryRepositoryService)
                patients = (repositoryService as IPersistableQueryRepositoryService).Find(queryExpr, offset, count, out totalCount, new Guid(queryId[0]));
            else
                patients = repositoryService.Find(queryExpr, offset, count, out totalCount);

            // Create care plan for the patients
            IConceptRepositoryService conceptService = ApplicationContext.Current.GetService<IConceptRepositoryService>();
            return patients.AsParallel().Select(o => {
                var plan = carePlanner.CreateCarePlan(o);
                foreach (var p in plan.Action)
                    p.Participations.ForEach(x => x.ParticipationRoleKey = x.ParticipationRoleKey ?? conceptService.GetConcept(x.ParticipationRole?.Mnemonic).Key);
                return plan;
            });

        }

        /// <summary>
        /// Update care plan 
        /// </summary>
        public Object Update(Object  data)
        {
            throw new NotSupportedException();
        }
    }
}
