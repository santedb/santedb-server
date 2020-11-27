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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Globalization;
using System.Linq;

using System.Security.Principal;

namespace SanteDB.Core
{
    /// <summary>
    /// Represents a series of extension methods for the <see cref="ApplicationServiceContext" /> class.
    /// </summary>
    public static class ExtensionMethods
	{
		/// <summary>
		/// Get application provider service
		/// </summary>
		/// <param name="me">The current application context.</param>
		/// <returns>Returns an instance of the <see cref="IApplicationIdentityProviderService"/>.</returns>
		public static IApplicationIdentityProviderService GetApplicationProviderService(this IApplicationServiceContext me)
		{
			return me.GetService<IApplicationIdentityProviderService>();
		}

		/// <summary>
		/// Gets the assigning authority repository service.
		/// </summary>
		/// <param name="me">The current application context.</param>
		/// <returns>Returns an instance of the <see cref="IAssigningAuthorityRepositoryService"/>.</returns>
		public static IAssigningAuthorityRepositoryService GetAssigningAuthorityService(this IApplicationServiceContext me)
		{
			return me.GetService<IAssigningAuthorityRepositoryService>();
		}

		/// <summary>
		/// Gets the business rules service for a specific information model.
		/// </summary>
		/// <typeparam name="T">The type of information for which to retrieve the business rules engine instance.</typeparam>
		/// <param name="me">The application context.</param>
		/// <returns>Returns an instance of the business rules service.</returns>
		public static IBusinessRulesService<T> GetBusinessRulesService<T>(this IApplicationServiceContext me) where T : IdentifiedData
		{
			return me.GetService<IBusinessRulesService<T>>();
		}

		/// <summary>
		/// Get the concept service.
		/// </summary>
		/// <param name="me">The current application context.</param>
		/// <returns>Returns an instance of the <see cref="IConceptRepositoryService"/>.</returns>
		public static IConceptRepositoryService GetConceptService(this IApplicationServiceContext me)
		{
			return me.GetService<IConceptRepositoryService>();
		}

		/// <summary>
		/// Gets the user identifier for a given identity.
		/// </summary>
		/// <returns>Returns a string which represents the users identifier, or null if unable to retrieve the users identifier.</returns>
		public static string GetUserId(IIdentity source)
		{
			return GetUserId<string>(source);
		}

		/// <summary>
		/// Gets the user identifier for a given identity.
		/// </summary>
		/// <typeparam name="T">The type of the identifier of the user.</typeparam>
		/// <returns>Returns the users identifier, or null if unable to retrieve the users identifier.</returns>
		public static T GetUserId<T>(IIdentity source) where T : IConvertible
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source), "Value cannot be null");
			}

			var userId = default(T);

			var nameIdentifierClaimValue = (source as IClaimsIdentity)?.Claims.FirstOrDefault(c => c.Type == SanteDBClaimTypes.NameIdentifier)?.Value;

			if (nameIdentifierClaimValue != null)
			{
				userId = (T)Convert.ChangeType(nameIdentifierClaimValue, typeof(T), CultureInfo.InvariantCulture);
			}

			return userId;
		}
        
        /// <summary>
        /// Convert to policy instance
        /// </summary>
        public static SecurityPolicyInstance ToPolicyInstance(this IPolicyInstance me)
        {
            return new SecurityPolicyInstance(
                new SecurityPolicy()
                {
                    CanOverride = me.Policy.CanOverride,
                    Oid = me.Policy.Oid,
                    Name = me.Policy.Name
                },
                (PolicyGrantType)(int)me.Rule
            );
        }
	}
}