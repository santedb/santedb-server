/*
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
using SanteDB.Core.Services;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Http;
using SanteDB.Core.Services;
using SanteDB.Messaging.GS1.Configuration;
using SanteDB.Messaging.GS1.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace SanteDB.Messaging.GS1.Transport.AS2
{
    /// <summary>
    /// GS1 Stock Integration Service
    /// </summary>
    [ServiceProvider("GS1 AS2(ish) Integration Service")]
    public class As2IntegrationService : IDaemonService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "GS1 AS.2 Integration Service";

        // The event handler
        private EventHandler<PersistentQueueEventArgs> m_handler;

        // Tracer
        private Tracer m_tracer = new Tracer(Gs1Constants.TraceSourceName);

        // Configuration
        private Gs1ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Gs1ConfigurationSection>();


        /// <summary>
        /// True when the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_handler != null;
            }
        }

        /// <summary>
        /// Fired when the service has completed startup
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Fired when the service is starting up
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Fired when the service has successfully stopped
        /// </summary>
        public event EventHandler Stopped;
        /// <summary>
        /// Fired when the service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Start the daemon service
        /// </summary>
        /// <returns></returns>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            // Create handler
            this.m_handler = (o, e) =>
            {
                do
                {
                    Object dq = null;
                    try
                    {
                        dq = ApplicationServiceContext.Current.GetService<IPersistentQueueService>().Dequeue(this.m_configuration.Gs1QueueName);
                        if (dq == null) break;
                        this.SendQueueMessage(dq);
                    }
                    catch (Exception ex)
                    {
                        this.m_tracer.TraceError(">>>> !!ALERT!! >>>> Error sending message to GS1 broker. Message will be placed in dead-letter queue");
                        this.m_tracer.TraceError(ex.ToString());
                        ApplicationServiceContext.Current.GetService<IPersistentQueueService>().Enqueue("dead", dq);
                    }
                } while (true);
            };

            // Queue Handler
            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                ApplicationServiceContext.Current.GetService<IPersistentQueueService>().Queued += this.m_handler;
                ApplicationServiceContext.Current.GetService<IPersistentQueueService>().Open(this.m_configuration.Gs1QueueName);

            };

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Get the message type and URL endpoint
        /// </summary>
        private void SendQueueMessage(object queueMessage)
        {
            try
            {
                this.m_tracer.TraceInfo("Dispatching message {0} to GS1 endpoint", queueMessage.GetType().Name);
                // First, we're going to create a rest client
                var restClient = new RestClient(this.m_configuration.Gs1BrokerAddress);
                if(!String.IsNullOrEmpty(this.m_configuration.Gs1BrokerAddress.UserName))
                    (restClient.Description.Binding as ServiceClientBindingDescription).Security = new As2BasicClientSecurityDescription(this.m_configuration.Gs1BrokerAddress);
                var client = new Gs1ServiceClient(restClient);

                if (queueMessage is OrderMessageType)
                    client.IssueOrder(queueMessage as OrderMessageType);
                else if (queueMessage is DespatchAdviceMessageType)
                    client.IssueDespatchAdvice(queueMessage as DespatchAdviceMessageType);
                else if (queueMessage is ReceivingAdviceMessageType)
                    client.IssueReceivingAdvice(queueMessage as ReceivingAdviceMessageType);
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Could not dispatch message to GS1 endpoint: {0}", e);
                throw;
            }
        }

        /// <summary>
        /// Stop the current service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            ApplicationServiceContext.Current.GetService<IPersistentQueueService>().Queued -= this.m_handler;
            this.m_handler = null;

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
