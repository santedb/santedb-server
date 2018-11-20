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
using RestSrvr.Attributes;
using SanteDB.Core.Model.RISI;
using System.IO;

namespace SanteDB.Messaging.RISI.Rest
{
	/// <summary>
	/// Provides operations for running and managing reports.
	/// </summary>
	[ServiceContractAttribute(Name = "RISI")]
	public partial interface IRisiContract
	{
		/// <summary>
		/// Creates a new parameter type.
		/// </summary>
		/// <param name="parameterType">The parameter type to create.</param>
		/// <returns>Returns the created parameter type.</returns>
		[RestInvoke(UriTemplate = "/type",  Method = "POST")]
		ParameterType CreateParameterType(ParameterType parameterType);

		/// <summary>
		/// Creates a new report definition.
		/// </summary>
		/// <param name="reportDefinition">The report definition to create.</param>
		/// <returns>Returns the created report definition.</returns>
		[RestInvoke(UriTemplate = "/report",  Method = "POST")]
        ReportDefinition CreateReportDefinition(ReportDefinition reportDefinition);

		/// <summary>
		/// Creates a report format.
		/// </summary>
		/// <param name="reportFormat">The report format to create.</param>
		/// <returns>Returns the created report format.</returns>
		[RestInvoke(UriTemplate = "/format",  Method = "POST")]
        ReportFormat CreateReportFormat(ReportFormat reportFormat);

		/// <summary>
		/// Deletes a report parameter type.
		/// </summary>
		/// <param name="id">The id of the report parameter type to delete.</param>
		/// <returns>Returns the deleted report parameter type.</returns>
		[RestInvoke(UriTemplate = "/type/{id}",  Method = "DELETE")]
        ParameterType DeleteParameterType(string id);

		/// <summary>
		/// Deletes a report definition.
		/// </summary>
		/// <param name="id">The id of the report definition to delete.</param>
		/// <returns>Returns the deleted report definition.</returns>
		[RestInvoke(UriTemplate = "/report/{id}",  Method = "DELETE")]
        ReportDefinition DeleteReportDefinition(string id);

		/// <summary>
		/// Deletes a report format.
		/// </summary>
		/// <param name="id">The id of the report format.</param>
		/// <returns>Returns the report deleted report format.</returns>
		[RestInvoke(UriTemplate = "/format/{id}",  Method = "DELETE")]
        ReportFormat DeleteReportFormat(string id);

		/// <summary>
		/// Gets a list of all report parameter types.
		/// </summary>
		/// <returns>Returns a list of report parameter types.</returns>
		[Get("/type")]
        RisiCollection<ParameterType> GetAllReportParameterTypes();

		/// <summary>
		/// Gets a report definition by id.
		/// </summary>
		/// <param name="id">The id of the report definition to retrieve.</param>
		/// <returns>Returns a report definition.</returns>
		[Get("/report/{id}")]
        ReportDefinition GetReportDefinition(string id);

		/// <summary>
		/// Gets a list of report definitions based on a specific query.
		/// </summary>
		/// <returns>Returns a list of report definitions.</returns>
		[Get("/report")]
        RisiCollection<ReportDefinition> GetReportDefinitions();

		/// <summary>
		/// Gets a report format by id.
		/// </summary>
		/// <param name="id">The id of the report format to retrieve.</param>
		/// <returns>Returns a report format.</returns>
		[Get("/format/{id}")]
        ReportFormat GetReportFormat(string id);

		/// <summary>
		/// Gets the report formats.
		/// </summary>
		/// <returns>Returns a list of report formats.</returns>
		[Get("/format")]
        RisiCollection<ReportFormat> GetReportFormats();

		/// <summary>
		/// Gets a report parameter by id.
		/// </summary>
		/// <param name="id">The id of the report parameter to retrieve.</param>
		/// <returns>Returns a report parameter.</returns>
		[Get("/type/{id}")]
		ReportParameter GetReportParameter(string id);

		/// <summary>
		/// Gets a list of report parameters.
		/// </summary>
		/// <param name="id">The id of the report for which to retrieve parameters.</param>
		/// <returns>Returns a list of parameters.</returns>
		[Get("/report/{id}/parm")]
		RisiCollection<ReportParameter> GetReportParameters(string id);

		/// <summary>
		/// Gets a list of auto-complete parameters which are applicable for the specified parameter.
		/// </summary>
		/// <param name="id">The id of the report.</param>
		/// <param name="parameterId">The id of the parameter for which to retrieve detailed information.</param>
		/// <returns>Returns an auto complete source definition of valid parameters values for a given parameter.</returns>
		[Get("/report/{id}/parm/{parameterId}/values")]
        AutoCompleteSourceDefinition GetReportParameterValues(string id, string parameterId);

		/// <summary>
		/// Gets a list of auto-complete parameters which are applicable for the specified parameter.
		/// </summary>
		/// <param name="id">The id of the report.</param>
		/// <param name="parameterId">The id of the parameter for which to retrieve detailed information.</param>
		/// <param name="parameterValue">The parameter value.</param>
		/// <returns>Returns an auto complete source definition of valid parameter values for a given parameter within the context of another parameter value.</returns>
		[Get("/report/{id}/parm/{parameterId}/values/{parameterValue}")]
		AutoCompleteSourceDefinition GetReportParameterValuesCascading(string id, string parameterId, string parameterValue);

		/// <summary>
		/// Gets the report source.
		/// </summary>
		/// <param name="id">The id of the report for which to retrieve the source.</param>
		/// <returns>Returns the report source.</returns>
		[Get("/report/{id}/source")]
		Stream GetReportSource(string id);

		/// <summary>
		/// Executes a report.
		/// </summary>
		/// <param name="id">The id of the report.</param>
		/// <param name="format">The output format of the report.</param>
		/// <param name="bundle">The report parameters.</param>
		/// <returns>Returns the report in raw format.</returns>
		[RestInvoke(UriTemplate = "/report/{id}/format/{format}",  Method = "POST")]
		Stream RunReport(string id, string format, ReportBundle bundle);

		/// <summary>
		/// Updates a parameter type definition.
		/// </summary>
		/// <param name="id">The id of the parameter type.</param>
		/// <param name="parameterType">The parameter type to update.</param>
		/// <returns>Returns the updated parameter type definition.</returns>
		[RestInvoke(UriTemplate = "/type/{id}",  Method = "PUT")]
		ParameterType UpdateParameterType(string id, ParameterType parameterType);

		/// <summary>
		/// Updates a report definition.
		/// </summary>
		/// <param name="id">The id of the report definition to update.</param>
		/// <param name="reportDefinition">The updated report definition.</param>
		/// <returns>Returns the updated report definition.</returns>
		[RestInvoke(UriTemplate = "/report/{id}",  Method = "PUT")]
		ReportDefinition UpdateReportDefinition(string id, ReportDefinition reportDefinition);

		/// <summary>
		/// Updates a report format.
		/// </summary>
		/// <param name="id">The id of the report format to update.</param>
		/// <param name="reportFormat">The updated report format.</param>
		/// <returns>Returns the update report format.</returns>
		[RestInvoke(UriTemplate = "/format/{id}",  Method = "PUT")]
        ReportFormat UpdateReportFormat(string id, ReportFormat reportFormat);
	}
}