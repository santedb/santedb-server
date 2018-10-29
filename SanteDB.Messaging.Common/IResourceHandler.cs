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
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using System;
using System.Collections.Generic;

namespace SanteDB.Messaging.Common
{
	/// <summary>
	/// Represents a resource handler.
	/// </summary>
	public interface IResourceHandler
	{
		/// <summary>
		/// Gets the name of the resource which the resource handler supports.
		/// </summary>
		string ResourceName { get; }

		/// <summary>
		/// Gets the type which the resource handler supports.
		/// </summary>
		Type Type { get; }

        /// <summary>
        /// Get the scope of this resource handler (the service to which the resources are bound)
        /// </summary>
        Type Scope { get; }

        /// <summary>
        /// Gets the capabilities of this service
        /// </summary>
        ResourceCapability Capabilities { get;  }

        /// <summary>
        /// Creates a resource.
        /// </summary>
        /// <param name="data">The resource data to be created.</param>
        /// <param name="updateIfExists">Updates the resource if the resource exists.</param>
        /// <returns>Returns the created resource.</returns>
        Object Create(Object data, bool updateIfExists);

        /// <summary>
        /// Gets a specific resource instance.
        /// </summary>
        /// <param name="id">The id of the resource.</param>
        /// <param name="versionId">The version id of the resource.</param>
        /// <returns>Returns the resource.</returns>
        Object Get(Object id, Object versionId);

        /// <summary>
        /// Obsoletes a resource.
        /// </summary>
        /// <param name="key">The key of the resource to obsolete.</param>
        /// <returns>Returns the obsoleted resource.</returns>
        Object Obsolete(Object key);

		/// <summary>
		/// Queries for a resource.
		/// </summary>
		/// <param name="queryParameters">The query parameters of the resource.</param>
		/// <returns>Returns a collection of resources.</returns>
		IEnumerable<Object> Query(NameValueCollection queryParameters);

		/// <summary>
		/// Queries for a resource.
		/// </summary>
		/// <param name="queryParameters">The query parameters of the resource.</param>
		/// <param name="offset">The offset of the query.</param>
		/// <param name="count">The count of the query.</param>
		/// <param name="totalCount">The total count of the results.</param>
		/// <returns>Returns a collection of resources.</returns>
		IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out Int32 totalCount);

        /// <summary>
        /// Updates a resource.
        /// </summary>
        /// <param name="data">The resource data to be updated.</param>
        /// <returns>Returns the updated resource.</returns>
        Object Update(Object data);
	}

    /// <summary>
    /// Represents a resource handler that can lock or unlock objects
    /// </summary>
    public interface ILockableResourceHandler : IResourceHandler
    {
        /// <summary>
        /// Locks a resource.
        /// </summary>
        /// <param name="key">The key of the resource to Locks.</param>
        /// <returns>Returns the locked object</returns>
        Object Lock(Object key);

        /// <summary>
        /// Obsoletes a unlock.
        /// </summary>
        /// <param name="key">The key of the resource to unlock.</param>
        /// <returns>Returns the unlock object.</returns>
        Object Unlock(Object key);
    }
}