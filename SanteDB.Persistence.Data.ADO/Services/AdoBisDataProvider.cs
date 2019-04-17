using SanteDB.BI;
using SanteDB.BI.Model;
using SanteDB.BI.Services;
using SanteDB.BI.Util;
using SanteDB.Core;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.OrmLite.Configuration;
using SanteDB.OrmLite.Providers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// ADO.NET BIS Data Source
    /// </summary>
    public class AdoBisDataProvider : IBisDataSource
    {
        // Tracer
        private Tracer m_tracer = new Tracer(AdoDataConstants.TraceSourceName);

        /// <summary>
        /// Executes the query
        /// </summary>
        public BisResultContext ExecuteQuery(BisQueryDefinition queryDefinition, Dictionary<string, object> parameters)
        {
            if (queryDefinition == null)
                throw new ArgumentNullException(nameof(queryDefinition));

            // Add core context parameters if not already added

            // First we want to grab the connection strings used by this object
            var filledQuery = BiUtils.ResolveRefs(queryDefinition);

            // The ADO.NET provider only allows one connection to one db at a time, so verify the connections are appropriate
            if (queryDefinition.DataSources?.Count != 1)
                throw new InvalidOperationException($"ADO.NET BI queries can only source data from 1 connection source, query {queryDefinition.Name} has {queryDefinition.DataSources?.Count}");

            // Ensure we have sufficient priviledge
            var pdpService = ApplicationServiceContext.Current.GetService<IPolicyDecisionService>();
            foreach (var pol in queryDefinition.DataSources.SelectMany(o => o.Demands).Union(queryDefinition.Demands))
            {
                var outcome = pdpService.GetPolicyOutcome(AuthenticationContext.Current.Principal, pol);
                if (outcome != Core.Model.Security.PolicyGrantType.Grant)
                    throw new PolicyViolationException(AuthenticationContext.Current.Principal, pol, outcome);
            }

            // Apply defaults where possible
            foreach (var defaultParm in queryDefinition.Parameters.Where(p => !String.IsNullOrEmpty(p.DefaultValue) && !parameters.ContainsKey(p.Name)))
                parameters.Add(defaultParm.Name, defaultParm.DefaultValue);

            // Next we validate parameters
            if (!queryDefinition.Parameters.Where(p => p.Required == true).All(p => parameters.ContainsKey(p.Name)))
                throw new InvalidOperationException("Missing required parameter");

            // Validate parameter values
            foreach (var kv in parameters.ToArray())
            {
                var parmDef = queryDefinition.Parameters.FirstOrDefault(p => p.Name == kv.Key);
                if (parmDef == null) continue; // skip
                else switch (parmDef.Type)
                    {
                        case BisParameterDataType.Boolean:
                            parameters[kv.Key] = Boolean.Parse(kv.Value.ToString());
                            break;
                        case BisParameterDataType.Date:
                        case BisParameterDataType.DateTime:
                            parameters[kv.Key] = DateTime.Parse(kv.Value.ToString());
                            break;
                        case BisParameterDataType.Integer:
                            parameters[kv.Key] = Int32.Parse(kv.Value.ToString());
                            break;
                        case BisParameterDataType.String:
                            parameters[kv.Key] = kv.Value.ToString();
                            break;
                        case BisParameterDataType.Uuid:
                            parameters[kv.Key] = Guid.Parse(kv.Value.ToString());
                            break;
                        default:
                            throw new InvalidOperationException($"Cannot determine how to parse {parmDef.Type}");
                    }
            }

            // We want to open the specified connection
            var connectionString = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<DataConfigurationSection>().ConnectionString.FirstOrDefault(o => o.Name == queryDefinition.DataSources.First().ConnectionString);
            var provider = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<OrmConfigurationSection>().GetProvider(connectionString.Provider);
            provider.ConnectionString = connectionString.Value;
            provider.ReadonlyConnectionString = connectionString.Value;

            // Query definition
            var rdbmsQueryDefinition = queryDefinition.QueryDefinitions.FirstOrDefault(o => o.Invariants.Contains(provider.Invariant));
            if (queryDefinition == null)
                throw new InvalidOperationException($"Could not find a query definition for invariant {provider.Invariant}");

            // Prepare the templated SQL
            var parmRegex = new Regex(@"\$\{([\w_][\d\w\._]*?)\}");
            List<Object> values = new List<object>();
            var stmt = parmRegex.Replace(rdbmsQueryDefinition.Sql, (m) =>
            {
                object pValue = null;
                parameters.TryGetValue(m.Groups[1].Value, out pValue);
                values.Add(pValue);
                return "?";
            });

            // Get a readonly context
            using (var context = provider.GetReadonlyConnection())
            {
                try
                {
                    context.Open();
                    DateTime startTime = DateTime.Now;
                    var results = context.Query<ExpandoObject>(new SqlStatement(provider, stmt, values.ToArray()));
                    return new BisResultContext(
                        queryDefinition,
                        parameters,
                        this,
                        results,
                        startTime);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error executing BIS data query: {0}", e);
                    throw new DataPersistenceException($"Error executing BIS data query", e);
                }
            }
        }

        /// <summary>
        /// Execute the specified query
        /// </summary>
        public BisResultContext ExecuteQuery(string queryId, Dictionary<string, object> parameters)
        {
            var query = ApplicationServiceContext.Current.GetService<IBisMetadataRepository>()?.Get<BisQueryDefinition>(queryId);
            if (query == null)
                throw new KeyNotFoundException(queryId);
            else
                return this.ExecuteQuery(query, parameters);
        }
    
    }
}
