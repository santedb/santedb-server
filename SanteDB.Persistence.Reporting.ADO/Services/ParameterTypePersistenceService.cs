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
using SanteDB.Core;
using SanteDB.Core.Model.RISI;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Persistence.Reporting.ADO.Services
{
    /// <summary>
    /// Represents a data type persistence service.
    /// </summary>
    public class ParameterTypePersistenceService : CorePersistenceService<ParameterType, ADO.Model.ParameterType, ADO.Model.ParameterType>
	{
		/// <summary>
		/// Maps a <see cref="ParameterType" /> instance to a <see cref="ADO.Model.ParameterType" /> instance.
		/// </summary>
		/// <param name="modelInstance">The model instance.</param>
		/// <param name="context">The context.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the mapped parameter type instance.</returns>
		public override object FromModelInstance(ParameterType modelInstance, DataContext context)
		{
			if (modelInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Warning, "Model instance is null, exiting map");
				return null;
			}

			this.traceSource.TraceEvent(EventLevel.Verbose, $"Mapping { nameof(ADO.Model.ParameterType) } to { nameof(ParameterType) }");

			return base.FromModelInstance(modelInstance, context);
		}

		/// <summary>
		/// Obsoletes the specified data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the obsoleted data.</returns>
		/// <exception cref="System.InvalidOperationException">Cannot obsolete report format which is currently in use</exception>
		public override ParameterType ObsoleteInternal(DataContext context, ParameterType model)
		{
			var parameterTypeService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ReportParameter>>();

			var results = parameterTypeService.Query(r => r.ParameterType.Key == model.Key, AuthenticationContext.Current.Principal);

			if (!results.Any())
			{
				return base.ObsoleteInternal(context, model);
			}

			throw new InvalidOperationException("Cannot obsolete parameter type which is currently in use");
		}

		/// <summary>
		/// Maps a <see cref="ADO.Model.ParameterType" /> instance to an <see cref="ParameterType" /> instance.
		/// </summary>
		/// <param name="domainInstance">The domain instance.</param>
		/// <param name="context">The context.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the mapped parameter type instance.</returns>
		/// <exception cref="System.ArgumentException">If the domain instance is not of the correct type</exception>
		public override ParameterType ToModelInstance(object domainInstance, DataContext context)
		{
			if (domainInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Warning, "Domain instance is null, exiting mapper");
				return null;
			}

			if (!(domainInstance is ADO.Model.ParameterType))
			{
				throw new ArgumentException($"Invalid type: {nameof(domainInstance)} is not of type {nameof(ADO.Model.ParameterType)}");
			}

			this.traceSource.TraceEvent(EventLevel.Verbose, $"Mapping { nameof(ParameterType) } to { nameof(ADO.Model.ParameterType) }");

			return base.ToModelInstance(domainInstance, context);
		}
	}
}