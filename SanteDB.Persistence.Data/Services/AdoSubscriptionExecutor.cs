using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Event;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Map;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Subscription;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.Configuration;
using SanteDB.Persistence.Data.Services.Persistence;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Linq;
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
        private readonly Regex m_parmRegex = new Regex(@"\$\{([\w_][\-\d\w\._]*?)\}", RegexOptions.Multiline);

        // Allowed target types
        private readonly Type[] m_allowedTypes = new Type[]
        {
            typeof(Entity),
            typeof(Act),
            typeof(Concept)
        };

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
        private readonly IServiceManager m_serviceManager;

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(AdoSubscriptionExecutor));

        /// <summary>
        /// Create the default subscription executor
        /// </summary>
        public AdoSubscriptionExecutor(IConfigurationManager configurationManager, ILocalizationService localizationService, IRepositoryService<SubscriptionDefinition> subscriptionDefinition, IServiceManager serviceManager)
        {
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
            this.m_subscriptionRepository = subscriptionDefinition;
            this.m_localizationService = localizationService;
            this.m_serviceManager = serviceManager;
            this.m_modelMapper = new ModelMapper(typeof(AdoPersistenceService).Assembly.GetManifestResourceStream(DataConstants.MapResourceName), "AdoModelMap");
        }

        /// <summary>
        /// Fired when the query is executed
        /// </summary>
        public event EventHandler<SubscriptionExecutedEventArgs> Executed;

        /// <summary>
        /// Fired when the query is about to execute
        /// </summary>
        public event EventHandler<SubscriptionExecutingEventArgs> Executing;

        /// <summary>
        /// Exectue the specified subscription
        /// </summary>
        public IQueryResultSet Execute(Guid subscriptionKey, NameValueCollection parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), ErrorMessages.ARGUMENT_NULL);
            }
            else if (subscriptionKey == Guid.Empty)
            {
                throw new ArgumentOutOfRangeException(nameof(subscriptionKey));
            }

            var subscription = ApplicationServiceContext.Current.GetService<IRepositoryService<SubscriptionDefinition>>()?.Get(subscriptionKey);
            if (subscription == null)
                throw new KeyNotFoundException(subscriptionKey.ToString());
            else
                return this.Execute(subscription, parameters);
        }

        /// <summary>
        /// Execute the current operation
        /// </summary>
        public IQueryResultSet Execute(SubscriptionDefinition subscription, NameValueCollection parameters)
        {
            if (subscription == null || subscription.LoadProperty(o => o.ServerDefinitions).Count == 0)
            {
                throw new InvalidOperationException(ErrorMessages.SUBSCRIPTION_MISSING_DEFINITION);
            }
            else if(!this.m_allowedTypes.Contains(subscription.ResourceType))
            {
                throw new InvalidOperationException(String.Format(ErrorMessages.SUBSCRIPTION_NOT_SUPPORTED_RESOURCE, String.Join(" or ", this.m_allowedTypes.Select(o => o.Name))));
            }
            try
            {

                var preArgs = new SubscriptionExecutingEventArgs(subscription, parameters, AuthenticationContext.Current.Principal);
                this.Executing?.Invoke(this, preArgs);
                if (preArgs.Cancel)
                {
                    this.m_tracer.TraceWarning("Pre-Event for executor indicates cancel");
                    return preArgs.Results;
                }

                // Subscriptions can execute against any type of data in SanteDB - so we want to get the appropriate persistence service
                var persistenceType = typeof(IDataPersistenceService<>).MakeGenericType(subscription.ResourceType);
                var persistenceInstance = ApplicationServiceContext.Current.GetService(persistenceType) as IQuerySetProvider;
                if (persistenceInstance == null)
                {
                    throw new InvalidOperationException(String.Format(ErrorMessages.SUBSCRIPTION_RESOURCE_NOSTORE, subscription.Resource));
                }
                // Get the definition
                var definition = subscription.ServerDefinitions.FirstOrDefault(o => o.InvariantName == m_configuration.Provider.Invariant);
                if (definition == null)
                {
                    throw new InvalidOperationException(String.Format(ErrorMessages.SUBSCRIPTION_NO_DEFINITION_FOR_PROVIDER, this.m_configuration.Provider.Invariant));
                }

                // No obsoletion time?
                if (typeof(IBaseData).IsAssignableFrom(subscription.ResourceType) && !parameters.TryGetValue("obsoletionTime", out _))
                    parameters.Add("obsoletionTime", "null");

                // Build the filter expression which is placed on the result set
                var queryExpression = QueryExpressionParser.BuildLinqExpression(subscription.ResourceType, parameters);

                var tableMapping = TableMapping.Get(this.m_modelMapper.MapModelType(subscription.ResourceType));

                // We want to build a query that is appropriate for the resource type so the definition will become
                // SELECT [columns for type] FROM (definition from subscription logic here) AS [tablename] WHERE [filter provided by caller];
                var query = new QueryBuilder(this.m_modelMapper, persistenceInstance.Provider).CreateQuery(subscription.ResourceType, queryExpression).Build();

                // Now we want to remove the portions of the built query statement after FROM and before WHERE as the definition in the subscription will be the source of our selection
                SqlStatement domainQuery = new SqlStatement(m_configuration.Provider, query.SQL.Substring(0, query.SQL.IndexOf(" FROM ")));

                // Append our query
                var definitionQuery = definition.Definition;
                var arguments = new List<Object>();
                definitionQuery = this.m_parmRegex.Replace(definitionQuery, (o) =>
                {
                    if (parameters.TryGetValue(o.Groups[1].Value, out var qValue))
                    {
                        Guid uuid = Guid.Empty;
                        if (Guid.TryParse(qValue.First(), out uuid))
                            arguments.AddRange(qValue.Select(v => Guid.Parse(v)).OfType<Object>());
                        else
                            arguments.AddRange(qValue);
                        return String.Join(",", qValue.Select(v => "?"));
                    }
                    return "NULL";
                });

                // Now we want to append the new definitional query (with parameter substitutions) to our main select statement
                domainQuery.Append(" FROM (").Append(definitionQuery, arguments.ToArray()).Append($") AS {tableMapping.TableName} ");
                domainQuery.Append(query.SQL.Substring(query.SQL.IndexOf("WHERE ")), query.Arguments.ToArray()); // Then we add the filters supplied by the caller 

                var retVal = persistenceInstance.Query(domainQuery);
                var postEvt = new SubscriptionExecutedEventArgs(subscription, parameters, retVal, AuthenticationContext.Current.Principal);
                this.Executed?.Invoke(this, postEvt);

                return postEvt.Results;

            }
            catch (DbException e)
            {
                this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "Data error executing subscription execution operation", subscription, e);
                throw e.TranslateDbException();
            }
            catch (Exception e)
            {
                this.m_tracer.TraceData(System.Diagnostics.Tracing.EventLevel.Error, "General error executing subscription execution operation", subscription, e);
                throw new DataPersistenceException(this.m_localizationService.GetString(ErrorMessageStrings.DATA_GENERAL), e);
            }
        }
    }
}
