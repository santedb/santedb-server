/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using MARC.Everest.Threading;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Represents a thread pool service
    /// </summary>
    [ServiceProvider("SanteDB PCL ThreadPool Provider")]
    public class ThreadPoolService : IDaemonService, IDisposable, IThreadPoolService
    {

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "SanteDB Thread Pool Provider";

        // Constructs a thread pool
        private WaitThreadPool m_threadPool = null;

        // Trace source
        private Tracer m_traceSource = new Tracer(SanteDBConstants.ServiceTraceSourceName);

        // Performance data
        private int m_nonPooledWorkers = 0;
        private int m_pooledWorkers = 0;
        private int m_errorWorkers = 0;

        /// <summary>
        /// Gets the concurrency of this thread pool
        /// </summary>
        internal int Concurrency { get; private set; }

        /// <summary>
        /// Gets the non-pooled threads
        /// </summary>
        internal int NonPooledWorkers => this.m_nonPooledWorkers;

        /// <summary>
        /// Gets the pooled workers
        /// </summary>
        internal int PooledWorkers => this.m_pooledWorkers;

        /// <summary>
        /// Gets the number of workers that crashed
        /// </summary>
        internal int ErroredWorkers => this.m_errorWorkers;

        /// <summary>
        /// True if the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_threadPool != null;
            }
        }

        /// <summary>
        /// Service has started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Service is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Service has stopped
        /// </summary>
        public event EventHandler Stopped;
        /// <summary>
        /// Service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Dispose this thread pool
        /// </summary>
        public void Dispose()
        {
            
            this.m_threadPool?.Dispose();
        }

        /// <summary>
        /// Queues a non-pooled work item
        /// </summary>
        public void QueueNonPooledWorkItem(Action<object> action, object parm)
        {
            Thread thd = new Thread(new ParameterizedThreadStart((o)=> {
                try
                {
                    Interlocked.Increment(ref m_nonPooledWorkers);
                    action(o);
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("THREAD DEATH: {0}", e);
                    Interlocked.Increment(ref m_errorWorkers);

                }
                finally
                {
                    Interlocked.Decrement(ref m_nonPooledWorkers);
                }
                }));
            thd.IsBackground = true;
            thd.Name = $"SanteDBBackground-{action}";
            thd.Start(parm);
        }

        /// <summary>
        /// Enqueues a user work item on the master thread pool
        /// </summary>
        /// <param name="action"></param>
        public void QueueUserWorkItem(Action<object> action)
        {
            this.m_threadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    Interlocked.Increment(ref m_pooledWorkers);
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.AnonymousPrincipal);
                    action(o);
                }
                catch (Exception e)
                {
                    Interlocked.Increment(ref m_errorWorkers);
                    this.m_traceSource.TraceError("THREAD DEATH: {0}", e);
                }
                finally
                {
                    Interlocked.Decrement(ref m_pooledWorkers);
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.AnonymousPrincipal);
                }
            });
        }

        /// <summary>
        /// Queue user work item
        /// </summary>
        public void QueueUserWorkItem(Action<object> action, object parm)
        {
            this.m_threadPool.QueueUserWorkItem((o) => {
                try
                {
                    Interlocked.Increment(ref m_pooledWorkers);
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.AnonymousPrincipal);
                    action(o);
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("THREAD DEATH: {0}", e);
                    Interlocked.Increment(ref m_errorWorkers);
                }
                finally
                {
                    Interlocked.Decrement(ref m_pooledWorkers);
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.AnonymousPrincipal);
                }
            }, parm);
        }

        /// <summary>
        /// Queue user work item
        /// </summary>
        public void QueueUserWorkItem(TimeSpan timeout, Action<object> action, object parm)
        {
            // Use timer service if it is available
            new Timer((o) => {
                try
                {
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.AnonymousPrincipal);
                    action(o);
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("THREAD DEATH: {0}", e);

                }
                finally
                {
                    AuthenticationContext.Current = new AuthenticationContext(AuthenticationContext.AnonymousPrincipal);
                }
            }, parm, (int)timeout.TotalMilliseconds, Timeout.Infinite);
        }

        /// <summary>
        /// Start
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            this.Concurrency = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<ApplicationServiceContextConfigurationSection>()?.ThreadPoolSize ?? Environment.ProcessorCount;
            
            if (this.m_threadPool != null)
                this.m_threadPool.Dispose();
            this.m_threadPool = new WaitThreadPool(this.Concurrency);

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            this.m_traceSource.TraceInfo("Waiting for thread pool work to finish...");
            try
            {
                this.m_threadPool.WaitOne(new TimeSpan(0, 0, 60), true);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Thread pool work could not complete in specified time. {0}", e.ToString());
            }

            this.m_threadPool.Dispose();
            this.m_threadPool = null;
            this.Stopped?.Invoke(this, EventArgs.Empty);

            return true;
        }
    }
}
