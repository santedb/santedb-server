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
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Model.RISI;
using SanteDB.Core.Model.Warehouse;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SanteDB.Messaging.RISI.Rest
{
    /// <summary>
    /// Represents the RISI behavior implementation
    /// </summary>
    public partial class RisiBehavior
	{
		/// <summary>
		/// Create datamart
		/// </summary>
        public DatamartDefinition CreateDatamart(DatamartDefinition definition)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			return adhocService.CreateDatamart(definition.Name, definition.Schema);
		}

        /// <summary>
        /// Create stored query
        /// </summary>
        public DatamartStoredQuery CreateStoredQuery(string datamartId, DatamartStoredQuery queryDefinition)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			return adhocService.CreateStoredQuery(Guid.Parse(datamartId), queryDefinition);
		}

        /// <summary>
        /// Create warehouse object
        /// </summary>
        public DataWarehouseObject CreateWarehouseObject(string datamartId, DataWarehouseObject obj)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			adhocService.Add(Guid.Parse(datamartId), obj.ToExpando());

			return obj;
		}

        /// <summary>
        /// Delete a datamart
        /// </summary>
        public void DeleteDatamart(string id)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			adhocService.DeleteDatamart(Guid.Parse(id));
		}

        /// <summary>
        /// Execute an ad-hoc query
        /// </summary>
        public RisiCollection<DataWarehouseObject> ExecuteAdhocQuery(string datamartId)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			return new RisiCollection<DataWarehouseObject>(adhocService.AdhocQuery(Guid.Parse(datamartId), RestOperationContext.Current.IncomingRequest.QueryString.ToQuery()).Select(o => new DataWarehouseObject(o)));
		}

        /// <summary>
        /// Execute a stored query
        /// </summary>
        public RisiCollection<DataWarehouseObject> ExecuteStoredQuery(string datamartId, string queryId)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

            int tr = 0;
            var qry = RestOperationContext.Current.IncomingRequest.QueryString.ToQuery();
            List<string> offset = null, count = null ;
            qry.TryGetValue("_offset", out offset);
            qry.TryGetValue("_count", out count);

            var res = adhocService.StoredQuery(Guid.Parse(datamartId), queryId, qry, Int32.Parse(offset?.FirstOrDefault() ?? "0"), Int32.Parse(count?.FirstOrDefault() ?? "100"),  out tr).Select(o => new DataWarehouseObject(o));

            return new RisiCollection<DataWarehouseObject>(res) { Size = tr };
		}

        /// <summary>
        /// Get a particular datamart
        /// </summary>
        public DatamartDefinition GetDatamart(string id)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			var retVal = adhocService.GetDatamart(Guid.Parse(id));
			if (retVal == null)
				throw new FileNotFoundException(id);
			return retVal;
		}

        /// <summary>
        /// Get all datamarts
        /// </summary>
        public RisiCollection<DatamartDefinition> GetDatamarts()
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			return new RisiCollection<DatamartDefinition>(adhocService.GetDatamarts());
		}

        /// <summary>
        /// Get stored queries for the specified datamart
        /// </summary>
        public RisiCollection<DatamartStoredQuery> GetStoredQueries(string datamartId)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			var dm = adhocService.GetDatamart(Guid.Parse(datamartId));
			if (dm == null)
				throw new FileNotFoundException(datamartId);

			return new RisiCollection<DatamartStoredQuery>(dm.Schema.Queries);
		}

        /// <summary>
        /// Get warehouse object
        /// </summary>
        public DataWarehouseObject GetWarehouseObject(string datamartId, string objectId)
		{
			var adhocService = ApplicationServiceContext.Current.GetService<IAdHocDatawarehouseService>();
			if (adhocService == null)
				throw new InvalidOperationException("Cannot find the adhoc data warehouse service");

			var retVal = adhocService.Get(Guid.Parse(datamartId), Guid.Parse(objectId));
			if (retVal == null)
				throw new FileNotFoundException(objectId);
			return retVal;
		}
	}
}