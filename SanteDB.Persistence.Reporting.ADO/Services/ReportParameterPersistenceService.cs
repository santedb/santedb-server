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
using SanteDB.Core.Model.RISI;
using SanteDB.Core.Model.RISI.Constants;
using SanteDB.OrmLite;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Persistence.Reporting.ADO.Services
{
    /// <summary>
    /// Represents a report persistence service.
    /// </summary>
    public class ReportParameterPersistenceService : CorePersistenceService<ReportParameter, ADO.Model.ReportParameter, ADO.Model.ReportParameter>
	{
		/// <summary>
		/// Converts a model instance to a domain instance.
		/// </summary>
		/// <param name="modelInstance">The model instance to convert.</param>
		/// <param name="context">The context.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the converted model instance.</returns>
		public override object FromModelInstance(ReportParameter modelInstance, DataContext context)
		{
			if (modelInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Warning, "Model instance is null, exiting map");
				return null;
			}

			this.traceSource.TraceEvent(EventLevel.Verbose, $"Mapping { nameof(ADO.Model.ReportParameter) } to { nameof(ReportParameter) }");

			var domainInstance = base.FromModelInstance(modelInstance, context) as Model.ReportParameter;

			if (modelInstance.ReportDefinitionKey != Guid.Empty)
			{
				domainInstance.ReportId = modelInstance.ReportDefinitionKey;
			}
			else
			{
				throw new InvalidOperationException("Cannot insert report parameter without report id");
			}

			domainInstance.ParameterTypeId = modelInstance.ParameterType?.Key ?? ParameterTypeKeys.Object;

			return domainInstance;
		}

		/// <summary>
		/// Gets a report parameter by correlation id.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="correlationId">The correlation identifier.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns a report parameter for a given correlation id.</returns>
		public ReportParameter Get(DataContext context, string correlationId)
		{
			int totalResults;
			return this.Query(context, r => r.CorrelationId == correlationId, 0, 1, out totalResults, false).FirstOrDefault();
		}

		/// <summary>
		/// Converts a domain instance to a model instance.
		/// </summary>
		/// <param name="domainInstance">The domain instance to convert.</param>
		/// <param name="context">The context.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the converted model instance.</returns>
		public override ReportParameter ToModelInstance(object domainInstance, DataContext context)
		{
			if (domainInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Warning, "Domain instance is null, exiting mapper");
				return null;
			}

			if (!(domainInstance is ADO.Model.ReportParameter))
			{
				throw new ArgumentException($"Invalid type: {nameof(domainInstance)} is not of type {nameof(ADO.Model.ReportParameter)}");
			}

			this.traceSource.TraceEvent(EventLevel.Verbose, $"Mapping { nameof(ReportParameter) } to { nameof(ADO.Model.ReportParameter) }");

			var modelInstance = base.ToModelInstance(domainInstance, context);

			modelInstance.ParameterType = new ParameterType(((Model.ReportParameter)domainInstance).ParameterTypeId);

			return modelInstance;
		}
	}
}