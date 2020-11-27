﻿/*
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
using SanteDB.OrmLite.Attributes;
using System;

namespace SanteDB.Persistence.Reporting.ADO.Model
{
    /// <summary>
    /// Represents a report parameter.
    /// </summary>
    [Table("report_parameter")]
	public class ReportParameter : DbIdentified
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ReportParameter"/> class.
		/// </summary>
		public ReportParameter() : this(Guid.NewGuid())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReportParameter"/> class.
		/// </summary>
		/// <param name="key">The key.</param>
		public ReportParameter(Guid key) : base(key)
		{
			this.CreationTime = DateTimeOffset.UtcNow;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReportParameter"/> class
		/// with a specific <see cref="Core.Model.RISI.ReportParameter"/> instance.
		/// </summary>
		/// <param name="reportParameter">The report parameter instance.</param>
		public ReportParameter(Core.Model.RISI.ReportParameter reportParameter) : this(reportParameter.Key.Value)
		{
			this.IsNullable = reportParameter.IsNullable;
			this.Name = reportParameter.Name;
			this.ParameterTypeId = reportParameter.ParameterType.Key.Value;
			this.Value = reportParameter.Value;
		}

		/// <summary>
		/// Gets or sets the correlation identifier.
		/// </summary>
		/// <value>The correlation identifier.</value>
		[NotNull]
		[Column("correlation_id")]
		public string CorrelationId { get; set; }

		/// <summary>
		/// Gets or sets the creation time of the parameter.
		/// </summary>
		[NotNull]
		[Column("creation_time")]
		public DateTimeOffset CreationTime { get; set; }

		/// <summary>
		/// Gets or sets the description of the report parameter.
		/// </summary>
		[Column("description")]
		public string Description { get; set; }

		/// <summary>
		/// Gets or sets whether the report parameter is nullable.
		/// </summary>
		[NotNull]
		[Column("is_nullable")]
		public bool IsNullable { get; set; }

		/// <summary>
		/// Gets or sets the key.
		/// </summary>
		/// <value>The key.</value>
		[PrimaryKey]
		[Column("id")]
		[AutoGenerated]
		public override Guid Key { get; set; }

		/// <summary>
		/// Gets or sets the name of the report parameter.
		/// </summary>
		[NotNull]
		[Column("name")]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the id of the parameter type associated with the report parameter.
		/// </summary>
		[Column("parameter_type_id")]
		[ForeignKey(typeof(ParameterType), nameof(ParameterType.Key))]
		public Guid ParameterTypeId { get; set; }

		/// <summary>
		/// Gets or sets the order of the report parameter.
		/// </summary>
		[NotNull]
		[Column("position")]
		public int Position { get; set; }

		/// <summary>
		/// Gets or sets the report id associated with the report parameter.
		/// </summary>
		[Column("report_id")]
		[ForeignKey(typeof(ReportDefinition), nameof(ReportDefinition.Key))]
		public Guid ReportId { get; set; }

		/// <summary>
		/// Get or set the value of the report parameter.
		/// </summary>
		[Column("value")]
		public byte[] Value { get; set; }
	}
}