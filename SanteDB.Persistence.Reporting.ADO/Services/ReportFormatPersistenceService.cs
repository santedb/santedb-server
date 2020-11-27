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
using SanteDB.Core;
using SanteDB.Core.Model.RISI;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using System;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Principal;

namespace SanteDB.Persistence.Reporting.ADO.Services
{
    /// <summary>
    /// Represents a report format persistence service.
    /// </summary>
    public class ReportFormatPersistenceService : CorePersistenceService<ReportFormat, ADO.Model.ReportFormat, ADO.Model.ReportFormat>
	{
		/// <summary>
		/// Converts a model instance to a domain instance.
		/// </summary>
		/// <param name="modelInstance">The model instance to convert.</param>
		/// <param name="context">The context.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the converted model instance.</returns>
		public override object FromModelInstance(ReportFormat modelInstance, DataContext context)
		{
			if (modelInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Warning, "Model instance is null, exiting map");
				return null;
			}

			this.traceSource.TraceEvent(EventLevel.Verbose, $"Mapping { nameof(ADO.Model.ReportFormat) } to { nameof(ReportFormat) }");

			return base.FromModelInstance(modelInstance, context);
		}

		/// <summary>
		/// Inserts the model.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the inserted model.</returns>
		/// <exception cref="DuplicateNameException">If the report format already exists</exception>
		public override ReportFormat InsertInternal(DataContext context, ReportFormat model)
		{
			int totalResults;

			var result = this.QueryInternal(context, o => o.Format == model.Format, 0, null, out totalResults, false).FirstOrDefault();

			if (result == null)
			{
				return base.InsertInternal(context, model);
			}

			throw new DuplicateNameException($"Cannot insert report format: {model.Format} because it already exists");
		}

		/// <summary>
		/// Obsoletes the specified data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the obsoleted data.</returns>
		/// <exception cref="System.InvalidOperationException">Cannot obsolete report format which is currently in use</exception>
		public override ReportFormat ObsoleteInternal(DataContext context, ReportFormat model)
		{
			var reportDefinitionService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<ReportDefinition>>();

			var results = reportDefinitionService.Query(r => r.Formats.Any(f => f.Format == model.Format), AuthenticationContext.Current.Principal);

			if (!results.Any())
			{
				return base.ObsoleteInternal(context, model);
			}

			throw new InvalidOperationException("Cannot obsolete report format which is currently in use");
		}

		/// <summary>
		/// Converts a domain instance to a model instance.
		/// </summary>
		/// <param name="domainInstance">The domain instance to convert.</param>
		/// <param name="context">The context.</param>
		/// <param name="overrideAuthContext">The principal to use instead of the default.</param>
		/// <returns>Returns the converted model instance.</returns>
		public override ReportFormat ToModelInstance(object domainInstance, DataContext context)
		{
			if (domainInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Warning, "Domain instance is null, exiting mapper");
				return null;
			}

			if (!(domainInstance is ADO.Model.ReportFormat))
			{
				throw new ArgumentException($"Invalid type: {nameof(domainInstance)} is not of type {nameof(ADO.Model.ReportFormat)}");
			}

			this.traceSource.TraceEvent(EventLevel.Verbose, $"Mapping { nameof(ReportFormat) } to { nameof(ADO.Model.ReportFormat) }");

			return base.ToModelInstance(domainInstance, context);
		}
	}
}