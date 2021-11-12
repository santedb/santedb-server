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
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

namespace SanteDB.Server.AdminConsole.Shell
{
    /// <summary>
    /// Represents the console trace writer
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ConsoleTraceWriter : TraceWriter
    {
        /// <summary>
        /// Console trace writer
        /// </summary>
        public ConsoleTraceWriter(string filter, IDictionary<String, EventLevel> sources) : base((EventLevel)Enum.Parse(typeof(EventLevel), filter), null, sources)
        {
        }
        
        /// <summary>
        /// Write trace to the console
        /// </summary>
        protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
        {
            switch (level)
            {
                case EventLevel.Critical:
                case EventLevel.Error:
                    Console.ForegroundColor = Console.BackgroundColor == ConsoleColor.Black ? ConsoleColor.Red : ConsoleColor.DarkRed;
                    Console.WriteLine($"E:{format}", args);
                    Console.ResetColor();
                    break;
                case EventLevel.Informational:
                    Console.ForegroundColor = Console.BackgroundColor == ConsoleColor.Black ? ConsoleColor.Cyan : ConsoleColor.DarkCyan;
                    Console.WriteLine($"I:{format}", args);
                    Console.ResetColor();
                    break;
                case EventLevel.Warning:
                    Console.ForegroundColor = Console.BackgroundColor == ConsoleColor.Black ? ConsoleColor.Yellow : ConsoleColor.DarkYellow;
                    Console.WriteLine($"W:{format}", args);
                    Console.ResetColor();
                    break;
                case EventLevel.Verbose:
                    Console.ForegroundColor = Console.BackgroundColor == ConsoleColor.Black ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta;
                    Console.WriteLine($"V:{format}", args);
                    Console.ResetColor();
                    break;

            }
        }
    }
}