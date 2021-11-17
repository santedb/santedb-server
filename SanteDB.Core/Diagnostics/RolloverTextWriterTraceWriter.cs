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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.Collections.Concurrent;
using SanteDB.Core.Diagnostics;
using System.ComponentModel;

namespace SanteDB.Server.Core.Diagnostics
{
    /// <summary>
    /// Timed Trace listener
    /// </summary>
    [DisplayName("File Trace Writer")]
    [Obsolete("Use SanteDB.Core.Diagnostics.Tracing.RolloverTextWriterTraceWriter", true)]
    public class RolloverTextWriterTraceWriter : SanteDB.Core.Diagnostics.Tracing.RolloverTextWriterTraceWriter
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public RolloverTextWriterTraceWriter(EventLevel filter, string fileName, IDictionary<string, EventLevel> sources) : base(filter, fileName, sources)
        {
        }
    }
}