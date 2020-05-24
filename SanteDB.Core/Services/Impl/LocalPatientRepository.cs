/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Model.Roles;
using System;
using System.Diagnostics;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local patient repository service
    /// </summary>
    public class LocalPatientRepository : GenericLocalNullifiedRepository<Patient>, IPatientRepositoryService, IRepositoryService<Patient>
	{
		
		/// <summary>
		/// The trace source instance.
		/// </summary>
		private readonly Tracer traceSource = new Tracer(SanteDBConstants.ServiceTraceSourceName);
        
		/// <summary>
		/// Merges two patients together
		/// </summary>
		/// <param name="survivor">The surviving patient record</param>
		/// <param name="victim">The victim patient record</param>
		/// <returns>A new version of patient <paramref name="a" /> representing the merge</returns>
		/// <exception cref="System.InvalidOperationException">If the persistence service is not found.</exception>
		/// <exception cref="System.NotImplementedException"></exception>
		public Patient Merge(Patient survivor, Patient victim)
		{
			var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException($"{nameof(IDataPersistenceService<Patient>)} not found");
			}

			var clientRegistryNotificationService = ApplicationServiceContext.Current.GetService<IClientRegistryNotificationService>();

			// TODO: Do this
			throw new NotImplementedException();
            
            clientRegistryNotificationService?.NotifyDuplicatesResolved(new NotificationEventArgs<Patient>(survivor));

        }

        /// <summary>
        /// Un-merge two patients
        /// </summary>
        public Patient UnMerge(Patient patient, Guid versionKey)
		{
			var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Patient>>();

			if (persistenceService == null)
			{
				throw new InvalidOperationException($"{nameof(IDataPersistenceService<Patient>)} not found");
			}

			throw new NotImplementedException();
		}
        
	}
}