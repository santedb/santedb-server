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
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Attribute;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using SanteDB.Core.Interop;
using SanteDB.Messaging.Common;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
	/// <summary>
	/// Resource handler which can deal with metadata resources
	/// </summary>
	public class AssigningAuthorityResourceHandler : IResourceHandler
	{
		// repository
		private IAssigningAuthorityRepositoryService m_repository;

		public AssigningAuthorityResourceHandler()
		{
			ApplicationContext.Current.Started += (o, e) => this.m_repository = ApplicationContext.Current.GetService<IAssigningAuthorityRepositoryService>();
                
		}

        /// <summary>
        /// Get the capabilities of this handler
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
        /// The name of the resource
        /// </summary>
        public string ResourceName
		{
			get
			{
				return "AssigningAuthority";
			}
		}

		/// <summary>
		/// Gets the type this resource handler exposes
		/// </summary>
		public Type Type
		{
			get
			{
				return typeof(AssigningAuthority);
			}
		}

		/// <summary>
		/// Create an assigning authority - not supported
		/// </summary>
		public Object Create(Object data, bool updateIfExists)
		{
			throw new NotSupportedException("Assigning Authorities cannot be created on HDSI use AMI");
		}

		/// <summary>
		/// Get the assigning authority
		/// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
		public Object Get(object id, object versionId)
		{
			return this.m_repository.Get((Guid)id);
		}

		/// <summary>
		/// Obsoletes an assigning authority
		/// </summary>
		public Object Obsolete(object key)
		{
            throw new NotSupportedException("Assigning Authorities cannot be obsoleted on HDSI use AMI");

        }

        /// <summary>
        /// Queries for assigning authority
        /// </summary>
        [PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
		public IEnumerable<Object> Query(NameValueCollection queryParameters)
		{
			return this.m_repository.Find(QueryExpressionParser.BuildLinqExpression<AssigningAuthority>(queryParameters));
		}

		/// <summary>
		/// Query for the specified AA
		/// </summary>
		[PolicyPermission(System.Security.Permissions.SecurityAction.Demand, PolicyId = PermissionPolicyIdentifiers.ReadMetadata)]
		public IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
		{
			return this.m_repository.Find(QueryExpressionParser.BuildLinqExpression<AssigningAuthority>(queryParameters), offset, count, out totalCount);
		}

		/// <summary>
		/// Update assigning authority
		/// </summary>
		public Object Update(Object  data)
		{
            throw new NotSupportedException("Assigning Authorities cannot be updated on HDSI use AMI");

        }
    }
}