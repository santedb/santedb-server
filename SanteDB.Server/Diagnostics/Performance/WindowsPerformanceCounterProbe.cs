/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2021-8-27
 */

using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Diagnostics.Performance
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
            var osiService = ApplicationServiceContext.Current.GetService<IOperatingSystemInfoService>();
            if (osiService.OperatingSystem == OperatingSystemID.Win32)
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
                if (this.m_windowsCounter != null)
                {
                    return this.m_windowsCounter.NextValue();
                }
                else
                {
                    return 0;
                }
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