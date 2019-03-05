/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Jobs;
using System;
using System.Collections.Generic;
using System.Timers;

namespace SanteDB.Caching.Memory
{
    /// <summary>
    /// Timer job that reduces pressure on the cache
    /// </summary>
    internal class CacheRegulatorTimerJob : IJob
    {

        /// <summary>
        /// Get the name of the service
        /// </summary>
        public string Name => "Memory Cache Pressure Reduction";

        /// <summary>
        /// Can Cancel?
        /// </summary>
        public bool CanCancel => false;

        /// <summary>
        /// Current status
        /// </summary>
        public JobStateType CurrentState { get; private set; }

        /// <summary>
        /// Get the parameters
        /// </summary>
        public IDictionary<string, Type> Parameters => null;

        /// <summary>
        /// Cancel
        /// </summary>
        public void Cancel()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Timer has elapsed
        /// </summary>
        public void Run(object sender, ElapsedEventArgs e, object[] parameters)
        {
            try
            {
                this.CurrentState = JobStateType.Running;
                MemoryCache.Current.ReducePressure();
                this.CurrentState = JobStateType.Completed;
            }
            catch
            {
                this.CurrentState = JobStateType.Aborted;
            }
        }
    }
}