using SanteDB.Caching.Redis.Configuration;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Caching.Redis
{
    /// <summary>
    /// Represents a REDIS based query persistence service
    /// </summary>
    [ServiceProvider("REDIS Query Persistence Service")]
    public class RedisQueryPersistenceService : IQueryPersistenceService, IDaemonService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "REDIS Query Persistence Service";

        /// <summary>
        /// True if service is running
        /// </summary>
        public bool IsRunning => this.m_configuration != null;

        // Redis trace source
        private Tracer m_tracer = new Tracer(RedisCacheConstants.TraceSourceName);

        // Connection
        private ConnectionMultiplexer m_connection;

        /// <summary>
        /// Query tag in a hash set
        /// </summary>
        private const int FIELD_QUERY_TAG_IDX = 0;
        /// <summary>
        /// Query total results 
        /// </summary>
        private const int FIELD_QUERY_TOTAL_RESULTS = 1;
        /// <summary>
        /// Query result index
        /// </summary>
        private const int FIELD_QUERY_RESULT_IDX = 2;


        // Configuration
        private RedisConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<RedisConfigurationSection>();

        /// <summary>
        /// Application daemon is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Application daemon has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Application is stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Application has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Add results to the query identifier
        /// </summary>
        public void AddResults(Guid queryId, IEnumerable<Guid> results)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                if (redisConn.KeyExists($"{queryId}.{FIELD_QUERY_RESULT_IDX}"))
                    redisConn.ListRightPush($"{queryId}.{FIELD_QUERY_RESULT_IDX}", results.Select(o => (RedisValue)o.ToByteArray()).ToArray(), flags: CommandFlags.FireAndForget);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetching results from REDIS: {0}", e);
                throw new Exception("Error fetching results from REDIS", e);
            }
        }

        /// <summary>
        /// Find query by identifier
        /// </summary>
        public Guid FindQueryId(object queryTag)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get query results
        /// </summary>
        public IEnumerable<Guid> GetQueryResults(Guid queryId, int offset, int count)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_RESULT_IDX}", this.m_configuration.TTL, CommandFlags.FireAndForget);
                redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}", this.m_configuration.TTL, CommandFlags.FireAndForget);
                if (redisConn.KeyExists($"{queryId}.{FIELD_QUERY_RESULT_IDX}"))
                    return redisConn.ListRange($"{queryId}.{FIELD_QUERY_RESULT_IDX}", offset, offset + count).Select(o => new Guid((byte[])o)).ToArray();
                else
                    return new Guid[0];
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetching results from REDIS: {0}", e);
                throw new Exception("Error fetching results from REDIS", e);
            }
        }

        /// <summary>
        /// Gets the query tag
        /// </summary>
        public object GetQueryTag(Guid queryId)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                return redisConn.StringGet($"{queryId}.{FIELD_QUERY_TAG_IDX}").ToString();
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error fetching: {0}", e);
                throw new Exception("Error fetching tag from REDIS", e);
            }
        }

        /// <summary>
        /// Determines if the query is registered
        /// </summary>
        public bool IsRegistered(Guid queryId)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                return redisConn.KeyExists($"{queryId}.{FIELD_QUERY_RESULT_IDX}");
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error fetching: {0}", e);
                throw new Exception("Error fetching from REDIS", e);
            }
        }

        /// <summary>
        /// Attempt to get the total result quantity
        /// </summary>
        public long QueryResultTotalQuantity(Guid queryId)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                var strTotalCount = redisConn.StringGet($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}");
                if(strTotalCount.HasValue)
                    return BitConverter.ToInt32(strTotalCount, 0);
                return 0;
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error getting query result quantity: {0}", e);
                throw new Exception("Error getting query result from REDIS", e);
            }
        }

        /// <summary>
        /// Registers the specified query result 
        /// </summary>
        public bool RegisterQuerySet(Guid queryId, IEnumerable<Guid> results, object tag, int totalResults)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                redisConn.KeyDelete($"{queryId}.{FIELD_QUERY_RESULT_IDX}");
                redisConn.ListRightPush($"{queryId}.{FIELD_QUERY_RESULT_IDX}", results.Select(o => (RedisValue)o.ToByteArray()).ToArray(), CommandFlags.FireAndForget);

                if (tag != null)
                {
                    redisConn.StringSet($"{queryId}.{FIELD_QUERY_TAG_IDX}", tag.ToString(), flags: CommandFlags.FireAndForget);
                    redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_TAG_IDX}", this.m_configuration.TTL, CommandFlags.FireAndForget);
                }

                redisConn.StringSet($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}", BitConverter.GetBytes(totalResults), flags: CommandFlags.FireAndForget);
                redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_RESULT_IDX}", this.m_configuration.TTL, CommandFlags.FireAndForget);
                redisConn.KeyExpire($"{queryId}.{FIELD_QUERY_TOTAL_RESULTS}", this.m_configuration.TTL, CommandFlags.FireAndForget);

                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error registering query in REDIS: {0}", e);
                throw new Exception("Error getting query result from REDIS", e);
            }
        }

        /// <summary>
        /// Sets the query tag if it exists
        /// </summary>
        public void SetQueryTag(Guid queryId, object value)
        {
            try
            {
                var redisConn = this.m_connection.GetDatabase(RedisCacheConstants.QueryDatabaseId);
                if (redisConn.KeyExists($"{queryId}.{FIELD_QUERY_RESULT_IDX}"))
                    redisConn.StringSet($"{queryId}.{FIELD_QUERY_TAG_IDX}", value?.ToString(), flags: CommandFlags.FireAndForget);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error setting tags in REDIS: {0}", e);
                throw new Exception("Error setting query tag in REDIS", e);
            }
        }

        /// <summary>
        /// Start the daemon
        /// </summary>
        public bool Start()
        {
            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                this.m_tracer.TraceInfo("Starting REDIS query service to hosts {0}...", String.Join(";", this.m_configuration.Servers));

                var configuration = new ConfigurationOptions()
                {
                    Password = this.m_configuration.Password
                };
                foreach (var itm in this.m_configuration.Servers)
                    configuration.EndPoints.Add(itm);

                this.m_connection = ConnectionMultiplexer.Connect(configuration);
                
                this.Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error starting REDIS query persistence, will switch to query persister : {0}", e);
                ApplicationServiceContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(RedisCacheService));
                ApplicationServiceContext.Current.GetService<IServiceManager>().RemoveServiceProvider(typeof(IDataCachingService));
                return false;
            }
        }

        /// <summary>
        /// Stops the connection broker
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);
            this.m_connection.Dispose();
            this.m_connection = null;
            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
