using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Subscription;
using SanteDB.Core.Services;
using SanteDB.Persistence.Data.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;

namespace SanteDB.Persistence.Data.Services
{
    /// <summary>
    /// An implementation of the <see cref="ISubscriptionExecutor"/> which uses an ADO persistence layer
    /// </summary>
    public class AdoSubscriptionExecutor : ISubscriptionExecutor
    {
        // Parameter regex
        private readonly Regex m_parmRegex = new Regex(@"(.*?)(\$\w*?\$)", RegexOptions.Multiline);

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Subscription Executor";

        // Ref to mapper
        private readonly ModelMapper m_modelMapper;

        // Gets the configuration for this object
        private readonly AdoPersistenceConfigurationSection m_configuration;

        // Subscription definition
        private readonly IRepositoryService<SubscriptionDefinition> m_subscriptionRepository;
        private readonly ILocalizationService m_localizationService;

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoSubscriptionExecutor));

        /// <summary>
        /// Create the default subscription executor
        /// </summary>
        public AdoSubscriptionExecutor(IConfigurationManager configurationManager, ILocalizationService localizationService, IRepositoryService<SubscriptionDefinition> subscriptionDefinition)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_subscriptionRepository = subscriptionDefinition;
            this.m_localizationService = localizationService;
            this.m_modelMapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(DataConstants.MapResourceName), "AdoModelMap");
        }

        /// <summary>
        /// Fired when the query is executed
        /// </summary>
        public event EventHandler<QueryResultEventArgs<IdentifiedData>> Executed;

        /// <summary>
        /// Fired when the query is about to execute
        /// </summary>
        public event EventHandler<QueryRequestEventArgs<IdentifiedData>> Executing;

        /// <summary>
        /// Exectue the specified subscription
        /// </summary>
        public IEnumerable<object> Execute(Guid subscriptionKey, NameValueCollection parameters, int offset, int? count, out int totalResults, Guid queryId)
        {
            var subscription = ApplicationServiceContext.Current.GetService<IRepositoryService<SubscriptionDefinition>>()?.Get(subscriptionKey);
            if (subscription == null)
                throw new KeyNotFoundException(subscriptionKey.ToString());
            else
                return this.Execute(subscription, parameters, offset, count, out totalResults, queryId);
        }

        /// <summary>
        /// Execute the current operation
        /// </summary>
        public IEnumerable<object> Execute(SubscriptionDefinition subscription, NameValueCollection parameters, int offset, int? count, out int totalResults, Guid queryId)
        {
            if (subscription == null || subscription.ServerDefinitions.Count == 0)
                throw new InvalidOperationException("Subscription does not have server definition");

            try
            {
                var preArgs = new QueryRequestEventArgs<IdentifiedData>(o => o.Key == subscription.Key, offset, count, queryId, AuthenticationContext.Current.Principal, new ModelSort<IdentifiedData>[0], parameters);
                this.Executing?.Invoke(this, preArgs);
                if (preArgs.Cancel)
                {
                    this.m_tracer.TraceWarning("Pre-Event for executor failed");
                    totalResults = preArgs.TotalResults;
                    return preArgs.Results;
                }

                var persistenceType = typeof(IDataPersistenceService<>).MakeGenericType(subscription.ResourceType);
                var persistenceInstance = ApplicationServiceContext.Current.GetService(persistenceType) as IAdoPersistenceService;
                var queryService = ApplicationServiceContext.Current.GetService<IQueryPersistenceService>();
                var cacheService = ApplicationServiceContext.Current.GetService<IDataCachingService>();

                // Get the definition
                var definition = subscription.ServerDefinitions.FirstOrDefault(o => o.InvariantName == m_configuration.Provider.Invariant);
                if (definition == null)
                    throw new InvalidOperationException($"Subscription does not provide definition for provider {m_configuration.Provider.Invariant}");

                // No obsoletion time?
                if (typeof(IBaseEntityData).IsAssignableFrom(subscription.ResourceType) && !parameters.ContainsKey("obsoletionTime"))
                    parameters.Add("obsoletionTime", "null");

                // Query expression
                var queryExpression = typeof(QueryExpressionParser).GetGenericMethod(
                    nameof(QueryExpressionParser.BuildLinqExpression),
                    new Type[] { subscription.ResourceType },
                    new Type[] { typeof(NameValueCollection) }
                ).Invoke(null, new object[] { parameters });

                // Query has been registered?
                IEnumerable<IdentifiedData> result = null;
                if (queryId != Guid.Empty && queryService?.IsRegistered(queryId) == true)
                {
                    totalResults = (int)queryService.QueryResultTotalQuantity(queryId);
                    result = queryService.GetQueryResults(queryId, offset, count ?? 100)
                        .Select(o =>
                        {
                            try
                            {
                                var retVal = cacheService.GetCacheItem(o);
                                if (retVal == null)
                                    using (var ctx = m_configuration.Provider.GetReadonlyConnection())
                                    {
                                        ctx.Open();
                                        ctx.LoadState = LoadState.FullLoad;
                                        retVal = persistenceInstance.Get(ctx, o) as IdentifiedData;
                                        cacheService?.Add(retVal);
                                    }
                                return retVal;
                            }
                            catch (Exception e)
                            {
                                this.m_tracer.TraceError("Error fetching query results for {0}: {1}", queryId, e);
                                throw new DataPersistenceException("Error fetching query results", e);
                            }
                        }).OfType<IdentifiedData>().ToList();
                }
                else
                {
                    // Now grab the context and query!!!
                    using (var connection = m_configuration.Provider.GetReadonlyConnection())
                    {
                        try
                        {
                            connection.Open();
                            connection.LoadState = LoadState.FullLoad;

                            // First, build the query using the query build
                            TableMapping tableMapping = null;
                            if (typeof(Entity).IsAssignableFrom(subscription.ResourceType))
                                tableMapping = TableMapping.Get(typeof(DbEntityVersion));
                            else if (typeof(Act).IsAssignableFrom(subscription.ResourceType))
                                tableMapping = TableMapping.Get(typeof(DbActVersion));
                            else if (typeof(Concept).IsAssignableFrom(subscription.ResourceType))
                                tableMapping = TableMapping.Get(typeof(DbConceptVersion));
                            else
                                throw new InvalidOperationException("ADO Subscriptions only support Entities and Acts (or sub-types)");

                            var query = (typeof(QueryBuilder).GetGenericMethod(
                                nameof(QueryBuilder.CreateQuery),
                                new Type[] { subscription.ResourceType },
                                new Type[] { queryExpression.GetType(), typeof(ColumnMapping).MakeArrayType() }
                            ).Invoke(this.m_queryBuilder, new object[] { queryExpression, tableMapping.Columns.ToArray() }) as SqlStatement).Build();

                            // Now we want to remove the portions of the built query statement after FROM and before WHERE as the definition will be the source of our selection
                            SqlStatement domainQuery = new SqlStatement(m_configuration.Provider, query.SQL.Substring(0, query.SQL.IndexOf(" FROM ")));

                            // Append our query
                            var definitionQuery = definition.Definition;
                            List<Object> values = new List<object>();
                            definitionQuery = this.m_parmRegex.Replace(definitionQuery, (o) =>
                            {
                                if (parameters.TryGetValue("_" + o.Groups[2].Value.Substring(1, o.Groups[2].Value.Length - 2), out List<String> qValue))
                                {
                                    Guid uuid = Guid.Empty;
                                    if (Guid.TryParse(qValue.First(), out uuid))
                                        values.AddRange(qValue.Select(v => Guid.Parse(v)).OfType<Object>());
                                    else
                                        values.AddRange(qValue);
                                    return o.Groups[1].Value + String.Join(",", qValue.Select(v => "?"));
                                }
                                return "NULL";
                            });

                            // Now we want to append
                            domainQuery.Append(" FROM (").Append(definitionQuery, values.ToArray()).Append($") AS {tableMapping.TableName} ");
                            domainQuery.Append(query.SQL.Substring(query.SQL.IndexOf("WHERE ")), query.Arguments.ToArray());

                            // Now we want to create the result type
                            var resultType = tableMapping.OrmType;
                            if (typeof(IDbVersionedData).IsAssignableFrom(resultType)) // type is versioned so we have to join
                            {
                                var fkType = tableMapping.GetColumn("Key").ForeignKey.Table;
                                resultType = typeof(CompositeResult<,>).MakeGenericType(resultType, fkType);
                            }

                            // Now we want to select out our results
                            if (count == 0)
                            {
                                totalResults = connection.Count(domainQuery);
                                return null;
                            }
                            else
                            {
                                // Fetch
                                var domainResults = typeof(DataContext).GetGenericMethod(
                                    nameof(DataContext.Query),
                                    new Type[] { resultType },
                                    new Type[] { typeof(SqlStatement) }).Invoke(connection, new object[] { domainQuery }) as IOrmResultSet;

                                IEnumerable<object> resultObjects = null;

                                // Register query if query id specified
                                if (queryId != Guid.Empty)
                                {
                                    var results = domainResults.Keys<Guid>().OfType<Guid>().ToArray();
                                    this.m_tracer.TraceVerbose("Query for Keys: {0}", connection.GetQueryLiteral(domainResults.Keys<Guid>().ToSqlStatement()));
                                    totalResults = results.Count();
                                    ApplicationServiceContext.Current.GetService<IQueryPersistenceService>()?.RegisterQuerySet(queryId, results, null, totalResults);
                                    resultObjects = results.Skip(offset).Take(count ?? 100).OfType<Object>();
                                }
                                else if (m_configuration.UseFuzzyTotals || preArgs.UseFuzzyTotals)
                                {
                                    this.m_tracer.TraceVerbose("Query for Objects: {0}", connection.GetQueryLiteral(domainResults.ToSqlStatement()));
                                    resultObjects = domainResults.Skip(offset).Take((count ?? 100) + 1).OfType<Object>();
                                    totalResults = domainResults.Count();
                                }
                                else
                                {
                                    this.m_tracer.TraceVerbose("Query for Objects: {0}", connection.GetQueryLiteral(domainResults.ToSqlStatement()));

                                    totalResults = domainResults.Count();
                                    resultObjects = domainResults.Skip(offset).Take(count ?? 100).OfType<Object>();
                                }
                                this.m_tracer.TraceVerbose("If i show up in the log, the log is ???????? WHY?????");
                                // Return
                                result = resultObjects
                                    .Take(count ?? 100)
                                .OfType<Object>()
                                .Select(o =>
                                {
                                    try
                                    {
                                        if (o is Guid)
                                        {
                                            var retVal = cacheService.GetCacheItem((Guid)o);
                                            if (retVal == null)
                                                using (var subConn = connection.OpenClonedContext())
                                                {
                                                    retVal = persistenceInstance.Get(subConn, (Guid)o) as IdentifiedData;
                                                    cacheService?.Add(retVal);
                                                }
                                            return retVal;
                                        }
                                        else
                                        {
                                            var idData = (o as CompositeResult)?.Values.OfType<IDbIdentified>().FirstOrDefault() ?? o as IDbIdentified;
                                            var retVal = cacheService.GetCacheItem(idData.Key);

                                            if (retVal == null)
                                                using (var subConn = connection.OpenClonedContext())
                                                {
                                                    retVal = persistenceInstance.ToModelInstance(o, subConn) as IdentifiedData;
                                                    cacheService?.Add(retVal);
                                                }
                                            return retVal;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        this.m_tracer.TraceError("Error converting result: {0}", e);
                                        throw;
                                    }
                                }).OfType<IdentifiedData>().ToList();
                            }
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            this.m_tracer.TraceError("Error executing subscription: {0}", e);
#else
                this.m_tracer.TraceError("Error executing subscription: {0}", e.Message);
#endif

                            throw new DataPersistenceException($"Error executing subscription: {e.Message}", e);
                        }
                    } // using conn
                } // if

                var postEvt = new QueryResultEventArgs<IdentifiedData>(o => o.Key == subscription.Key, result, offset, count, totalResults, queryId, AuthenticationContext.Current.Principal);
                this.Executed?.Invoke(this, postEvt);

                // Now set the overridden data
                return postEvt.Results;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error executing core ADO Subscription logic for {0}: {1}", subscription.Key, e);
                throw new Exception($"Error executing core ADO subscription logic for {subscription.Key}", e);
            }
        }
    }
}
