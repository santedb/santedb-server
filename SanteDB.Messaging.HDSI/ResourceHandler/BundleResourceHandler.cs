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
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using SanteDB.Core.Interop;
using SanteDB.Messaging.Common;

namespace SanteDB.Messaging.HDSI.ResourceHandler
{
	/// <summary>
	/// Represents a resource handler which is for the persistence of bundles.
	/// </summary>
	public class BundleResourceHandler : IResourceHandler
	{
		/// <summary>
		/// The internal reference to the <see cref="IBatchRepositoryService"/> instance.
		/// </summary>
		private IBatchRepositoryService repositoryService;

		/// <summary>
		/// Initializes a new instance of the <see cref="BundleResourceHandler"/> class.
		/// </summary>
		public BundleResourceHandler()
		{
			ApplicationContext.Current.Started += (o, e) => this.repositoryService = ApplicationContext.Current.GetService<IBatchRepositoryService>();
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
                return ResourceCapability.Update | ResourceCapability.Create;
            }
        }

        /// <summary>
        /// Gets the resource name which this resource handler handles.
        /// </summary>
        public string ResourceName => "Bundle";

		/// <summary>
		/// Gets the type which this resource handler handles.
		/// </summary>
		public Type Type => typeof(Bundle);

		/// <summary>
		/// Creates a bundle.
		/// </summary>
		/// <param name="data">The data to create.</param>
		/// <param name="updateIfExists">Whether to update an existing entity.</param>
		/// <returns>Returns the created bundle.</returns>
		public Object Create(Object data, bool updateIfExists)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}
			var bundle = data as Bundle;

			if (bundle == null)
			{
				throw new ArgumentException("Bundle required", nameof(data));
			}

			//bundle.Reconstitute();

			if (updateIfExists)
			{
				return this.repositoryService.Update(bundle);
			}
			else
			{
				// Submit
				return this.repositoryService.Insert(bundle);
			}
		}

		/// <summary>
		/// Gets the specified data
		/// </summary>
		public Object Get(Guid id, Guid versionId)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Obsoletes the bundle
		/// </summary>
		public Object Obsolete(Guid  key)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Query for bundle
		/// </summary>
		public IEnumerable<Object> Query(NameValueCollection queryParameters)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Query bundle
		/// </summary>
		public IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Updates a specific bundle.
		/// </summary>
		/// <param name="data">The data to be updated.</param>
		/// <returns>Returns the updated data.</returns>
		public Object Update(Object  data)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			var bundle = data as Bundle;

			if (bundle == null)
			{
				throw new ArgumentException("Bundle required", nameof(data));
			}

			// Submit
			return this.repositoryService.Update(bundle);
		}
	}
}