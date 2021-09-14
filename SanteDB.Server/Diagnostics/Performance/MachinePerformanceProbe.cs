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
using SanteDB.Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Diagnostics.Performance
{
    /// <summary>
    /// A machine performance probe
    /// </summary>
    public class MachinePerformanceProbe : ICompositeDiagnosticsProbe
    {

        private IDiagnosticsProbe[] m_values =
        {
            new WindowsPerformanceCounterProbe(SanteDBConstants.ProcessorUseCounter, "Machine: CPU Utilization", "Shows the % of active time for CPU", "Processor Information", "% Processor Time", "_Total"),
            new WindowsPerformanceCounterProbe(SanteDBConstants.MemoryUseCounter, "Machine: Memory Use", "Shows the amount of memory used", "Memory", "% Committed Bytes In Use", null)
        };


        /// <summary>
        /// Gets the value of this probe
        /// </summary>
        public IEnumerable<IDiagnosticsProbe> Value => this.m_values;

        /// <summary>
        /// Gets the identifier of this counter
        /// </summary>
        public Guid Uuid => SanteDBConstants.MachinePerformanceCounter;

        /// <summary>
        /// Gets the name of this counter
        /// </summary>
        public string Name => "Machine";

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description => "Shows metrics related to the server host environment";

        /// <summary>
        /// Gets the type of measure
        /// </summary>
        public Type Type => typeof(Array);

        /// <summary>
        /// Gets the value
        /// </summary>
        object IDiagnosticsProbe.Value => this.Value;
    }
}
