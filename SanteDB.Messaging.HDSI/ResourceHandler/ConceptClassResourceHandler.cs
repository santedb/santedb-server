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
using MARC.HI.EHRS.SVC.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using SanteDB.Core.Interop;
using SanteDB.Messaging.Common;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
	/// <summary>
	/// Represents concept class resource handler.
	/// </summary>
	public class ConceptClassResourceHandler : IResourceHandler
	{
		private IConceptRepositoryService repository;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConceptClassResourceHandler"/> class.
		/// </summary>
		public ConceptClassResourceHandler()
		{
			ApplicationContext.Current.Started += (o, e) => this.repository = ApplicationContext.Current.GetService<IConceptRepositoryService>();
		}

        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(Wcf.IHdsiServiceContract);

        /// <summary>
        /// Get capabilities
        /// </summary>
        public ResourceCapability Capabilities
        {
            get
            {
                return ResourceCapability.Get | ResourceCapability.Search;
            }
        }

        /// <summary>
        /// Gets the resource name.
        /// </summary>
        public string ResourceName
		{
			get
			{
				return "ConceptClass";
			}
		}

		/// <summary>
		/// Gets the resource type.
		/// </summary>
		public Type Type
		{
			get
			{
				return typeof(ConceptClass);
			}
		}

		/// <summary>
		/// Creates an organization.
		/// </summary>
		/// <param name="data">The organization to be created.</param>
		/// <param name="updateIfExists">Update the organization if it exists.</param>
		/// <returns>Returns the newly create organization.</returns>
		public Object Create(Object data, bool updateIfExists)
		{
			Bundle bundleData = data as Bundle;
			bundleData?.Reconstitute();
			var processData = bundleData?.Entry ?? data;

			if (processData is Bundle)
			{
				throw new InvalidOperationException(string.Format("Bundle must have entry of type {0}", nameof(ConceptClass)));
			}
			else if (processData is ConceptClass)
			{
				var conceptClassData = data as ConceptClass;

				if (updateIfExists)
				{
					return this.repository.SaveConceptClass(conceptClassData);
				}
				else
				{
					return this.repository.InsertConceptClass(conceptClassData);
				}
			}
			else
			{
				throw new ArgumentException("Invalid persistence type");
			}
		}

		/// <summary>
		/// Gets an organization by id and version id.
		/// </summary>
		/// <param name="id">The id of the organization.</param>
		/// <param name="versionId">The version id of the organization.</param>
		/// <returns>Returns the organization.</returns>
		public Object Get(Guid id, Guid versionId)
		{
			return this.repository.GetConceptClass(id);
		}

		/// <summary>
		/// Obsoletes an organization.
		/// </summary>
		/// <param name="key">The key of the organization to obsolete.</param>
		/// <returns>Returns the obsoleted organization.</returns>
		public Object Obsolete(Guid  key)
		{
			return this.repository.ObsoleteConceptClass(key);
		}

		/// <summary>
		/// Queries for an organization.
		/// </summary>
		/// <param name="queryParameters">The query parameters for which to use to query for the organization.</param>
		/// <returns>Returns a list of organizations.</returns>
		public IEnumerable<Object> Query(NameValueCollection queryParameters)
		{
            int tr = 0;
			return this.Query(queryParameters, 0, 100, out tr);
		}

		/// <summary>
		/// Queries for an organization.
		/// </summary>
		/// <param name="queryParameters">The query parameters for which to use to query for the organization.</param>
		/// <param name="offset">The query offset.</param>
		/// <param name="count">The count of the query.</param>
		/// <param name="totalCount">The total count of the query.</param>
		/// <returns>Returns a list of organizations.</returns>
		public IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
		{
            var filter = QueryExpressionParser.BuildLinqExpression<ConceptClass>(queryParameters);
            List<String> queryId = null;
            if (this.repository is IPersistableQueryRepositoryService && queryParameters.TryGetValue("_queryId", out queryId))
                return (this.repository as IPersistableQueryRepositoryService).Find(filter, offset, count, out totalCount, Guid.Parse(queryId[0]));
            else
                return this.repository.FindConceptClasses(filter, offset, count, out totalCount);
		}

		/// <summary>
		/// Updates an organization.
		/// </summary>
		/// <param name="data">The organization to be updated.</param>
		/// <returns>Returns the updated organization.</returns>
		public Object Update(Object  data)
		{
			Bundle bundleData = data as Bundle;
			bundleData?.Reconstitute();
			var processData = bundleData?.Entry ?? data;

			if (processData is Bundle)
			{
				throw new InvalidOperationException(string.Format("Bundle must have entry of type {0}", nameof(ConceptClass)));
			}
			else if (processData is ConceptClass)
			{
				var conceptClassData = data as ConceptClass;

				return this.repository.SaveConceptClass(conceptClassData);
			}
			else
			{
				throw new ArgumentException("Invalid persistence type");
			}
		}
	}
}