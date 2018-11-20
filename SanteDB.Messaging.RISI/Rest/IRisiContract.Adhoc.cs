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
using SanteDB.Core.Model.RISI;
using SanteDB.Core.Model.Warehouse;
using System;
using System.ServiceModel;
using RestSrvr;
using RestSrvr.Attributes;

namespace SanteDB.Messaging.RISI.Rest
{
	/// <summary>
	/// RISI contract members for the data-warehouse
	/// </summary>
	public partial interface IRisiContract
	{
		/// <summary>
		/// Create a datamart
		/// </summary>
		[RestInvoke(Method = "POST", UriTemplate = "/datamart")]
		DatamartDefinition CreateDatamart(DatamartDefinition definition);

		/// <summary>
		/// Create a stored query
		/// </summary>
		[RestInvoke(Method = "POST", UriTemplate = "/datamart/{datamartId}/query")]
		DatamartStoredQuery CreateStoredQuery(String datamartId, DatamartStoredQuery queryDefinition);

		/// <summary>
		/// Create warehouse object
		/// </summary>
		[RestInvoke(Method = "POST", UriTemplate = "/datamart/{datamartId}/data")]
		DataWarehouseObject CreateWarehouseObject(String datamartId, DataWarehouseObject obj);

		/// <summary>
		/// Delete data mart
		/// </summary>
		[RestInvoke(Method = "DELETE", UriTemplate = "/datamart/{id}")]
		void DeleteDatamart(String id);

		/// <summary>
		/// Execute adhoc query
		/// </summary>
		[Get("/datamart/{datamartId}/data")]
		RisiCollection<DataWarehouseObject> ExecuteAdhocQuery(String datamartId);

		/// <summary>
		/// Executes a stored query
		/// </summary>
		[Get("/datamart/{datamartId}/query/{queryId}")]
		RisiCollection<DataWarehouseObject> ExecuteStoredQuery(String datamartId, String queryId);

		/// <summary>
		/// Gets a specified datamart
		/// </summary>
		[Get("/datamart/{id}")]
		DatamartDefinition GetDatamart(String id);

		/// <summary>
		/// Gets a list of all datamarts from the warehouse
		/// </summary>
		[Get("/datamart")]
		RisiCollection<DatamartDefinition> GetDatamarts();

		/// <summary>
		/// Get stored queries
		/// </summary>
		[Get("/datamart/{datamartId}/query")]
		RisiCollection<DatamartStoredQuery> GetStoredQueries(String datamartId);

		/// <summary>
		/// Get warehouse object
		/// </summary>
		[Get("/datamart/{datamartId}/data/{objectId}")]
		DataWarehouseObject GetWarehouseObject(String datamartId, String objectId);
	}
}