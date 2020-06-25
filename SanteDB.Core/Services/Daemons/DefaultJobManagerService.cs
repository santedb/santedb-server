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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;
using System.Diagnostics;
using System.Timers;
using System.ComponentModel;
using SanteDB.Core.Jobs;
using SanteDB.Core.Configuration;

namespace SanteDB.Core.Services
{
    /// <summary>
    /// Represents the default implementation of the timer
    /// </summary>
    [ServiceProvider("Default Job Manager", Configuration = typeof(JobConfigurationSection))]
    public class DefaultJobManagerService  : IJobManagerService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Default Job Manager";

        /// <summary>
        /// Timer configuration
        /// </summary>
        private JobConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<JobConfigurationSection>();

        /// <summary>
        /// Timer thread
        /// </summary>
        private System.Timers.Timer[] m_timers;

        /// <summary>
        /// Timer service is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Timer service is stopping
        /// </summary>
        public event EventHandler Stopping;
        /// <summary>
        /// Timer service is started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Timer service is stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Log of timers
        /// </summary>
        private Dictionary<IJob, DateTime> m_log = new Dictionary<IJob, DateTime>();

        /// <summary>
        /// Creates a new instance of the timer
        /// </summary>
        public DefaultJobManagerService()
        {
            
        }

        #region ITimerService Members

        /// <summary>
        /// Start the timer
        /// </summary>
        public bool Start()
        {

            Trace.TraceInformation("Starting timer service...");

            // Invoke the starting event handler
            this.Starting?.Invoke(this, EventArgs.Empty);

            // Setup timers based on the jobs
            this.m_timers = new System.Timers.Timer[this.m_configuration.Jobs.Count];
            int i = 0;
            foreach (var job in this.m_configuration.Jobs)
            {
                var jobInstance = Activator.CreateInstance(job.Type) as IJob;
                if (jobInstance == null)
                    throw new InvalidOperationException($"{job.Type} does not implement IJob");

                lock (this.m_log)
                    this.m_log.Add(jobInstance, DateTime.MinValue);

                // Timer setup
                if (job.Interval != -1) // Not a real job just run on demand
                {
                    var timer = new System.Timers.Timer(job.Interval)
                    {
                        AutoReset = true,
                        Enabled = true
                    };
                    timer.Elapsed += this.CreateElapseHandler(jobInstance);
                    timer.Start();
                    this.m_timers[i++] = timer;

                    // Fire the job at startup
                    jobInstance.Run(timer, null, null);
                }

            }

            this.Started?.Invoke(this, EventArgs.Empty);

            Trace.TraceInformation("Timer service started successfully");
            return true;
        }

        /// <summary>
        /// Create a time elapsed handler
        /// </summary>
        private ElapsedEventHandler CreateElapseHandler(IJob job)
        {
            return new System.Timers.ElapsedEventHandler((o, e) =>
            {

                // Log that the timer fired
                if (this.m_log.ContainsKey(job))
                    this.m_log[job] = e.SignalTime;
                else
                    lock (this.m_log)
                        this.m_log.Add(job, e.SignalTime);

                job.Run(o, e, null);

            });
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        public bool Stop()
        {
            // Stop all timers
            Trace.TraceInformation("Stopping timer service...");
            this.Stopping?.Invoke(this, EventArgs.Empty);

            if(this.m_timers != null)
                foreach (var timer in this.m_timers)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            this.m_timers = null;

            this.Stopped?.Invoke(this, EventArgs.Empty);

            Trace.TraceInformation("Timer service stopped successfully");
            return true;
        }

        /// <summary>
        /// Add a job
        /// </summary>
        public void AddJob(IJob jobObject, TimeSpan elapseTime, JobStartType startType = JobStartType.Immediate)
        {
            if (!(jobObject is IJob))
                throw new ArgumentOutOfRangeException(nameof(jobObject));

            lock (this.m_log)
                this.m_log.Add(jobObject, DateTime.MinValue);

            // Resize the timer array
            if (elapseTime != TimeSpan.MaxValue)
            {
                lock (this.m_timers)
                {
                    Array.Resize(ref this.m_timers, this.m_timers.Length + 1);
                    var timer = new System.Timers.Timer(elapseTime.TotalMilliseconds)
                    {
                        AutoReset = true,
                        Enabled = true
                    };
                    timer.Elapsed += this.CreateElapseHandler(jobObject);
                    timer.Start();
                    this.m_timers[this.m_timers.Length - 1] = timer;
                }

                if(startType == JobStartType.Immediate)
                    jobObject.Run(this, null, null);

            }

        }

        /// <summary>
        /// Return true if job object is registered
        /// </summary>
        public bool IsJobRegistered(Type jobObject)
        {
            return this.m_log.Keys.Any(o => o.GetType() == jobObject);
        }

        /// <summary>
        /// Returns true when the service is running
        /// </summary>
        public bool IsRunning { get { return this.m_timers != null; } }

        /// <summary>
        /// Get the jobs
        /// </summary>
        public IEnumerable<IJob> Jobs
        {
            get
            {
                return this.m_log.Keys;
            }
        }

        /// <summary>
        /// Get the last time the job was run
        /// </summary>
        public DateTime? GetLastRuntime(IJob job)
        {
            if (!this.m_log.TryGetValue(job, out DateTime retVal))
                return null;
            return retVal;
        }

        /// <summary>
        /// Start a job right now
        /// </summary>
        public void StartJob(IJob job, object[] parameters)
        {
            // Log that the timer fired
            if (this.m_log.ContainsKey(job))
                this.m_log[job] = DateTime.Now;
            else
                lock (this.m_log)
                    this.m_log.Add(job, DateTime.Now);

            ApplicationServiceContext.Current.GetService<IThreadPoolService>().QueueUserWorkItem((o)=> job.Run(this, EventArgs.Empty, parameters));
        }

        /// <summary>
        /// Get the specified job instance
        /// </summary>
        public IJob GetJobInstance(String jobTypeName)
        {
            return this.m_log.Keys.First(o => o.GetType().FullName == jobTypeName);
        }
        #endregion
    }
}
