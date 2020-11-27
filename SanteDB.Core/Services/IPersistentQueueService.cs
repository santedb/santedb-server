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
using System;

namespace SanteDB.Core.Services
{

    /// <summary>
    /// Represents event args related to a queue event
    /// </summary>
    public class PersistentQueueEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the name of the queue
        /// </summary>
        public String QueueName { get; private set; }

        /// <summary>
        /// Get the correlation token or object that was provided by the queue
        /// </summary>
        public Object Data { get; private set; }

        /// <summary>
        /// Create a new persistence queue event arg instance
        /// </summary>
        /// <param name="queueName">The name of the queue</param>
        /// <param name="data">The data in the queue</param>
        public PersistentQueueEventArgs(String queueName, object data)
        {
            this.Data = data;
            this.QueueName = queueName;
        }
    }

    /// <summary>
    /// Represents a persistent queue where data can be stored and retrieved
    /// </summary>
    public interface IPersistentQueueService : IServiceImplementation
    {
        /// <summary>
        /// Opens the specified queue name and enables subscriptions
        /// </summary>
        void Open(String queueName);

        /// <summary>
        /// Enqueue the specified data to the persistent queue
        /// </summary>
        void Enqueue(String queueName, Object data);

        /// <summary>
        /// Dequeues the last added item from the persistent queue
        /// </summary>
        Object Dequeue(String queueName);

        /// <summary>
        /// Fired when an item was queued to a queue
        /// </summary>
        event EventHandler<PersistentQueueEventArgs> Queued;
    }
}
