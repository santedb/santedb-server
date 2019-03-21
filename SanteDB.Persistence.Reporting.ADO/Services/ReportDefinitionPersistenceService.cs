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
using SanteDB.OrmLite;
using SanteDB.Persistence.Reporting.ADO.Model;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Principal;
using ReportDefinition = SanteDB.Core.Model.RISI.ReportDefinition;

namespace SanteDB.Persistence.Reporting.ADO.Services
{
    /// <summary>
    /// Represents a ReportDefinition persistence service.
    /// </summary>
    public class ReportDefinitionPersistenceService : CorePersistenceService<ReportDefinition, Model.ReportDefinition, Model.ReportDefinition>
	{
		/// <summary>
		/// Converts a model instance to a domain instance.
		/// </summary>
		/// <param name="modelInstance">The model instance to convert.</param>
		/// <param name="context">The context.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the converted model instance.</returns>
		public override object FromModelInstance(ReportDefinition modelInstance, DataContext context)
		{
			if (modelInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Warning, "Model instance is null, exiting map");
				return null;
			}

			this.traceSource.TraceEvent(EventLevel.Verbose, $"Mapping { nameof(ADO.Model.ReportDefinition) } to { nameof(ReportDefinition) }");

			return base.FromModelInstance(modelInstance, context);
		}

        /// <summary>
        /// Gets the specified model.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <param name="principal">The principal.</param>
        /// <param name="loadFast">if set to <c>true</c> [load fast].</param>
        /// <returns>Returns the model instance.</returns>
        public override ReportDefinition Get(DataContext context, Guid key, bool loadFast, IPrincipal overrideContext = null)
		{
			var reportDefinition = base.Get(context, key, loadFast);

			if (reportDefinition == null)
			{
				return null;
			}

			if (!loadFast)
			{
				reportDefinition.Parameters = context.Query<Model.ReportParameter>(r => r.ReportId == key).Select(r => new Core.Model.RISI.ReportParameter
				{
					Key = r.Key,
					Name = r.Name,
					CorrelationId = r.CorrelationId,
					Description = r.Description,
					Position = r.Position
				}).ToList();
			}

			return reportDefinition;
		}

		/// <summary>
		/// Inserts the model.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the inserted model.</returns>
		/// <exception cref="System.InvalidOperationException">Domain instance must not be null</exception>
		public override ReportDefinition InsertInternal(DataContext context, ReportDefinition model)
		{
			var domainInstance = this.FromModelInstance(model, context) as Model.ReportDefinition;

			if (domainInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Error,  "Domain instance must not be null");
				throw new InvalidOperationException("Domain instance must not be null");
			}

			if (domainInstance.Author == null)
			{
				domainInstance.Author = "SYSTEM";
			}

			domainInstance = context.Insert(domainInstance);

			model.Key = domainInstance.Key;

			InsertReportFormatAssociations(context, model);
			UpdateReportParameters(context, model);

			return this.ToModelInstance(domainInstance, context);
		}

		/// <summary>
		/// Obsoletes the specified data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the obsoleted data.</returns>
		public override ReportDefinition ObsoleteInternal(DataContext context, ReportDefinition model)
		{
			// delete the report format associations
			context.Delete<ReportDefinitionFormatAssociation>(c => c.SourceKey == model.Key.Value);

			// delete the report parameter associations
			context.Delete<Model.ReportParameter>(c => c.ReportId == model.Key.Value);

			// delete the actual report definition
			base.ObsoleteInternal(context, model);

			return model;
		}

		/// <summary>
		/// Converts a domain instance to a model instance.
		/// </summary>
		/// <param name="domainInstance">The domain instance to convert.</param>
		/// <param name="context">The context.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the converted model instance.</returns>
		public override ReportDefinition ToModelInstance(object domainInstance, DataContext context)
		{
			if (domainInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Warning, "Domain instance is null, exiting mapper");
				return null;
			}

			if (!(domainInstance is ADO.Model.ReportDefinition))
			{
				throw new ArgumentException($"Invalid type: {nameof(domainInstance)} is not of type {nameof(ADO.Model.ReportDefinition)}");
			}

			this.traceSource.TraceEvent(EventLevel.Verbose, $"Mapping { nameof(ReportDefinition) } to { nameof(ADO.Model.ReportDefinition) }");

			return base.ToModelInstance(domainInstance, context);
		}

		/// <summary>
		/// Updates the specified storage data.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="model">The model.</param>
		/// <param name="principal">The principal.</param>
		/// <returns>Returns the updated model instance.</returns>
		/// <exception cref="System.InvalidOperationException">Domain instance must not be null</exception>
		public override ReportDefinition UpdateInternal(DataContext context, ReportDefinition model)
		{
			var domainInstance = base.FromModelInstance(model, context) as Model.ReportDefinition;

			if (domainInstance == null)
			{
				this.traceSource.TraceEvent(EventLevel.Error,  "Domain instance must not be null");
				throw new InvalidOperationException("Domain instance must not be null");
			}

			if (domainInstance.Author == null)
			{
				domainInstance.Author = "SYSTEM";
			}

			domainInstance = context.Update(domainInstance);

			model.Key = domainInstance.Key;

			InsertReportFormatAssociations(context, model);
			UpdateReportParameters(context, model);

			return this.ToModelInstance(domainInstance, context);
		}

		/// <summary>
		/// Updates the report formats.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="reportDefinition">The report definition.</param>
		private static void InsertReportFormatAssociations(DataContext context, ReportDefinition reportDefinition)
		{
			foreach (var reportFormat in reportDefinition.Formats)
			{
				var existing = context.Query<ReportDefinitionFormatAssociation>(c => c.Key == reportFormat.Key.Value && c.SourceKey == reportDefinition.Key.Value).FirstOrDefault();

				if (existing != null)
				{
					context.Delete<ReportDefinitionFormatAssociation>(c => c.Key == reportFormat.Key.Value && c.SourceKey == reportDefinition.Key.Value);
				}

				context.Insert(new ReportDefinitionFormatAssociation(reportFormat.Key.Value, reportDefinition.Key.Value));
			}
		}

		/// <summary>
		/// Updates the report parameters.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="principal">The principal.</param>
		/// <param name="reportDefinition">The report definition.</param>
		private static void UpdateReportParameters(DataContext context, ReportDefinition reportDefinition)
		{
			var reportParameterPersistenceService = new ReportParameterPersistenceService();

			foreach (var reportParameter in reportDefinition.Parameters)
			{
				var existingReportParameter = context.Query<Model.ReportParameter>(c => c.ReportId == reportDefinition.Key.Value && c.CorrelationId == reportParameter.CorrelationId).FirstOrDefault();

				reportParameter.ReportDefinitionKey = reportDefinition.Key.Value;

				if (existingReportParameter == null)
				{
					reportParameterPersistenceService.Insert(context, reportParameter);
				}
				else
				{
					reportParameterPersistenceService.Update(context, reportParameter);
				}
			}
		}
	}
}