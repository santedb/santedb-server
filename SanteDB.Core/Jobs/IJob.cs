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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace SanteDB.Core.Jobs
{
    /// <summary>
    /// Represents a timer job
    /// </summary>
    public interface IJob
    {

        /// <summary>
        /// The name of the job
        /// </summary>
        String Name { get; }

        /// <summary>
        /// True if the job can be cancelled
        /// </summary>
        bool CanCancel { get;  }

        /// <summary>
        /// Execute the job
        /// </summary>
        void Run(object sender, ElapsedEventArgs e, object[] parameters);

        /// <summary>
        /// Cancel the job
        /// </summary>
        void Cancel();

        /// <summary>
        /// Gets the current status of the job
        /// </summary>
        JobStateType CurrentState { get; }

        /// <summary>
        /// Get the parameter definitions
        /// </summary>
        IDictionary<String, Type> Parameters { get; }
    }
}
