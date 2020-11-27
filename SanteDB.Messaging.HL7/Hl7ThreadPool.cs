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
 * Date: 2020-9-11
 */
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HL7
{
    /// <summary>
    /// Represents a rest server thread pool
    /// </summary>
    internal sealed class HL7ThreadPool : IDisposable
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(HL7ThreadPool));

        // Number of threads to keep alive
        private int m_concurrencyLevel = System.Environment.ProcessorCount * 2;

        // Queue of work items
        private Queue<WorkItem> m_queue = null;

        // Active threads
        private Thread[] m_threadPool = null;
        // Hint of the number of threads waiting to be executed
        private int m_threadWait = 0;
        // True when the thread pool is being disposed
        private bool m_disposing = false;

        /// <summary>
        /// Concurrency
        /// </summary>
        public int Concurrency { get { return this.m_concurrencyLevel; } }

        // Get current thread pool
        private static HL7ThreadPool s_current;

        /// <summary>
        /// Get the singleton threadpool
        /// </summary>
        public static HL7ThreadPool Current
        {
            get
            {
                if (s_current == null)
                    s_current = new HL7ThreadPool();
                return s_current;
            }
        }

        /// <summary>
        /// Creates a new instance of the wait thread pool
        /// </summary>
        private HL7ThreadPool()
        {
            this.m_queue = new Queue<WorkItem>(this.m_concurrencyLevel);
        }

        /// <summary>
        /// Worker data structure
        /// </summary>
        private struct WorkItem
        {
            /// <summary>
            /// The callback to execute on the worker
            /// </summary>
            public Action<Object> Callback { get; set; }
            /// <summary>
            /// The state or parameter to the worker
            /// </summary>
            public object State { get; set; }
            /// <summary>
            /// The execution context
            /// </summary>
            public ExecutionContext ExecutionContext { get; set; }
        }

        // Number of remaining work items
        private int m_remainingWorkItems = 1;

        // Thread is done reset event
        private ManualResetEventSlim m_threadDoneResetEvent = new ManualResetEventSlim(false);

        /// <summary>
        /// Queue a work item to be completed
        /// </summary>
        public void QueueUserWorkItem(Action<Object> callback)
        {
            QueueUserWorkItem(callback, null);
        }

        /// <summary>
        /// Queue a user work item with the specified parameters
        /// </summary>
        public void QueueUserWorkItem(Action<Object> callback, object state)
        {
            this.QueueWorkItemInternal(callback, state);
        }

        /// <summary>
        /// Perform queue of workitem internally
        /// </summary>
        private void QueueWorkItemInternal(Action<Object> callback, object state)
        {
            ThrowIfDisposed();

            try
            {
                WorkItem wd = new WorkItem()
                {
                    Callback = callback,
                    State = state,
                    ExecutionContext = ExecutionContext.Capture()
                };
                lock (this.m_threadDoneResetEvent) this.m_remainingWorkItems++;
                this.EnsureStarted(); // Ensure thread pool threads are started
                lock (m_queue)
                {
                    m_queue.Enqueue(wd);

                    if (m_threadWait > 0)
                        Monitor.Pulse(m_queue);
                }
            }
            catch (Exception e)
            {
                try
                {
                    this.m_tracer.TraceEvent(System.Diagnostics.Tracing.EventLevel.Error, "Error queueing work item: {0}", e);
                }
                catch { }
            }
        }

        /// <summary>
        /// Ensure the thread pool threads are started
        /// </summary>
        private void EnsureStarted()
        {
            if (m_threadPool == null)
            {
                lock (m_queue)
                    if (m_threadPool == null)
                    {
                        m_threadPool = new Thread[m_concurrencyLevel];
                        for (int i = 0; i < m_threadPool.Length; i++)
                        {
                            m_threadPool[i] = new Thread(DispatchLoop);
                            m_threadPool[i].Name = String.Format("HL7-ThreadPoolThread-{0}", i);
                            m_threadPool[i].IsBackground = true;
                            m_threadPool[i].Start();
                        }
                    }
            }
        }

        /// <summary>
        /// Dispatch loop
        /// </summary>
        private void DispatchLoop()
        {
            while (true)
            {
                WorkItem wi = default(WorkItem);
                lock (m_queue)
                {
                    try
                    {
                        if (m_disposing) return; // Shutdown requested
                        while (m_queue.Count == 0)
                        {
                            m_threadWait++;
                            try { Monitor.Wait(m_queue); }
                            finally { m_threadWait--; }
                            if (m_disposing)
                                return;
                        }
                        wi = m_queue.Dequeue();
                    }
                    catch (Exception e)
                    {
                        this.m_tracer.TraceError("Error in dispatchloop {0}", e);
                    }
                }
                DoWorkItem(wi);
            }
        }


        /// <summary>
        /// Wait until the thread is complete
        /// </summary>
        /// <returns></returns>
        public bool WaitOne() { return WaitOne(-1); }

        /// <summary>
        /// Wait until the thread is complete or the specified timeout elapses
        /// </summary>
        public bool WaitOne(TimeSpan timeout)
        {
            return WaitOne((int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Wait until the thread is completed
        /// </summary>
        public bool WaitOne(int timeout)
        {
            ThrowIfDisposed();
            DoneWorkItem();
            bool rv = this.m_threadDoneResetEvent.Wait(timeout);
            lock (this.m_threadDoneResetEvent)
            {
                if (rv)
                {
                    this.m_remainingWorkItems = 1;
                    this.m_threadDoneResetEvent.Reset();
                }
                else this.m_remainingWorkItems++;
            }
            return rv;
        }

        /// <summary>
        /// Perform the work if the specified work data
        /// </summary>
        private void DoWorkItem(WorkItem state)
        {
            try
            {
                this.m_tracer.TraceVerbose("Starting task on {0} ---> {1}", Thread.CurrentThread.Name, state.Callback.Target.ToString());
                var worker = (WorkItem)state;
                worker.Callback(worker.State);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceVerbose("!!!!!! 0118 999 881 999 119 7253 : THREAD DEATH !!!!!!!\r\nUncaught Exception on worker thread: {0}", e);
            }
            finally
            {
                DoneWorkItem();
            }
        }

        /// <summary>
        /// Complete a workf item
        /// </summary>
        private void DoneWorkItem()
        {
            try
            {
                // Finished invokation
                lock (this.m_threadDoneResetEvent)
                {
                    --this.m_remainingWorkItems;
                    if (this.m_remainingWorkItems == 0) this.m_threadDoneResetEvent.Set();
                }
            }
            catch (Exception e)
            {
                try
                {
                    this.m_tracer.TraceVerbose("!!!!!! ERROR REMOVING ITEM FROM THREAD POOL QUEUE", e);
                }
                catch { }
            }
        }

        /// <summary>
        /// Throw an exception if the object is disposed
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (this.m_threadDoneResetEvent == null) throw new ObjectDisposedException(this.GetType().Name);
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (this.m_threadDoneResetEvent != null)
            {
                if (this.m_remainingWorkItems > 0)
                    this.WaitOne();

                ((IDisposable)m_threadDoneResetEvent).Dispose();
                this.m_threadDoneResetEvent = null;
                m_disposing = true;
                lock (m_queue)
                    Monitor.PulseAll(m_queue);

                if (m_threadPool != null)
                    for (int i = 0; i < m_threadPool.Length; i++)
                    {
                        m_threadPool[i].Join();
                        m_threadPool[i] = null;
                    }
            }
        }

        #endregion

    }
}
