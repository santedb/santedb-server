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
 * Date: 2019-12-24
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Diagnostics.Performance
{
    /// <summary>
    /// Allows the measuring of processor time
    /// </summary>
    public class WindowsPerformanceCounterProbe : DiagnosticsProbeBase<float>, IDisposable
    {


        // Windows counter
        private PerformanceCounter m_windowsCounter = null;

        /// <summary>
        /// Processor time performance probe
        /// </summary>
        public WindowsPerformanceCounterProbe(Guid uuid, String name, String description, String category, String measure, String value) : base(name, description)
        {
            if (ApplicationContext.Current.OperatingSystem == OperatingSystemID.Win32)
            {
                this.m_windowsCounter = new PerformanceCounter(category, measure, value, true);
            }
            this.Uuid = uuid;
        }

        /// <summary>
        /// Gets the current value
        /// </summary>
        public override float Value
        {
            get
            {
                return this.m_windowsCounter?.NextValue() ?? 0;
            }
        }

        /// <summary>
        /// Gets the UUID for the counter
        /// </summary>
        public override Guid Uuid { get; }

        /// <summary>
        /// Dispose the counter
        /// </summary>
        public void Dispose()
        {
            this.m_windowsCounter.Dispose();
        }
    }
}