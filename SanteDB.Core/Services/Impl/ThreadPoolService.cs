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
        private WaitThreadPool m_threadPool = new WaitThreadPool();

        private TraceSource m_traceSource = new TraceSource(SanteDBConstants.ServiceTraceSourceName);

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
            Thread thd = new Thread(new ParameterizedThreadStart(action));
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

            int concurrency = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SanteDBServerConfiguration>()?.ThreadPoolSize ?? Environment.ProcessorCount;
            if (this.m_threadPool != null)
                this.m_threadPool.Dispose();
            this.m_threadPool = new WaitThreadPool(concurrency);

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
