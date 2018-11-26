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
 * User: justin
 * Date: 2018-6-22
 */
using SanteDB.OrmLite.Migration;
using System;
using System.IO;
using System.Text;

namespace SanteDB.Persistence.Data.ADO.Data.SQL
{
    /// <summary>
    /// Represents a data definition feature
    /// </summary>
    public class AdoCoreDataFeature : IDataFeature
    {
        /// <summary>
        /// Gets or sets the name of the feature
        /// </summary>
        public string Name
        {
            get
            {
                return "SanteDB Core Deployment";
            }
        }

        /// <summary>
        /// Get the check sql
        /// </summary>
        public string GetCheckSql(string invariantName)
        {
            return "SELECT false;";
        }

        /// <summary>
        /// Get deployment sql
        /// </summary>
        public string GetDeploySql(string invariantName)
        {
            String[] resource = new String[]
            {
                "SanteDB-ddl.sql",
                "SanteDB-fn.sql"
            };
            StringBuilder sql = new StringBuilder();

            // Build sql
            switch (invariantName.ToLower())
            {
                case "npgsql":

                    foreach (var itm in resource)
                        using (var streamReader = new StreamReader(typeof(AdoCoreDataFeature).Assembly.GetManifestResourceStream($"SanteDB.Persistence.Data.ADO.Data.SQL.PSQL.{itm}")))
                            sql.Append(streamReader.ReadToEnd());
                    return sql.ToString();
                case "fbsql":
                    foreach (var itm in resource)
                        using (var streamReader = new StreamReader(typeof(AdoCoreDataFeature).Assembly.GetManifestResourceStream($"SanteDB.Persistence.Data.ADO.Data.SQL.FBSQL.{itm}")))
                            sql.Append(streamReader.ReadToEnd());
                    return sql.ToString();
                default:
                    throw new InvalidOperationException($"Deployment for {invariantName} not supported");
            }
        }

        /// <summary>
        /// Get un-deploy sql
        /// </summary>
        public string GetUnDeploySql(string invariantName)
        {
            // Build sql
            switch (invariantName.ToLower())
            {
                case "npgsql":
                    using (var streamReader = new StreamReader(typeof(AdoCoreDataFeature).Assembly.GetManifestResourceStream($"SanteDB.Persistence.Data.ADO.Data.SQL.PSQL.SanteDB-drop.sql")))
                        return streamReader.ReadToEnd();
                default:
                    throw new InvalidOperationException($"Deployment for {invariantName} not supported");
            }
        }
    }
}
