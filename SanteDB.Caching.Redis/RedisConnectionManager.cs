using SanteDB.Caching.Redis.Configuration;
using SanteDB.Core;
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
    /// REDIS Connection manager which keeps a multiplexor 
    /// </summary>
    internal class RedisConnectionManager : IDisposable
    {

        /// <summary>
        ///  Current connection manager
        /// </summary>
        private static RedisConnectionManager m_current;

        // Lock
        private static object m_lock = new object();

        // Connection manager
        private ConnectionMultiplexer m_connection;
        // Subscriber
        private ISubscriber m_subscriber;
        // Configuration section
        private RedisConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<RedisConfigurationSection>();

        /// <summary>
        /// Ctor for singleton
        /// </summary>
        private RedisConnectionManager()
        {
            var configuration = new ConfigurationOptions()
            {
                Password = this.m_configuration.Password
            };
            foreach (var itm in this.m_configuration.Servers)
                configuration.EndPoints.Add(itm);

            this.m_connection = ConnectionMultiplexer.Connect(configuration);
            this.m_subscriber = this.m_connection.GetSubscriber();
        }

        /// <summary>
        /// Connection multiplexer
        /// </summary>
        public ConnectionMultiplexer Connection => this.m_connection;

        /// <summary>
        /// Get subscriber
        /// </summary>
        public ISubscriber Subscriber => this.m_subscriber;

        /// <summary>
        /// Gets the current instance
        /// </summary>
        public static RedisConnectionManager Current
        {
            get
            {
                if (m_current == null)
                    lock (m_lock)
                        if (m_current == null)
                            m_current = new RedisConnectionManager();
                return m_current;
            }
        }

        /// <summary>
        /// Dispose the connection
        /// </summary>
        public void Dispose()
        {
            this.m_connection.Dispose();
        }
    }
}
