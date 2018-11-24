﻿/*
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core
{
    /// <summary>
    /// SanteDB constants
    /// </summary>
    internal static class SanteDBConstants
    {

        // Configuration name
        internal const string SanteDBConfigurationName = "santedb.core";

        // Security trace source
        internal const string SecurityTraceSourceName = "SanteDB.Core.Security";

        // Map trace source
        internal const string MapTraceSourceName= "SanteDB.Core.Map";

        /// <summary>
        /// SanteDB dataset installation source name
        /// </summary>
        internal const string DatasetInstallSourceName = "SanteDB.Core.DataSet";

        // Client claim header
        internal const string BasicHttpClientClaimHeaderName = "X-SanteDBClient-Claim";
        // Client auth header
        internal const string BasicHttpClientCredentialHeaderName = "X-SanteDBClient-Authorization";

        // Device authorization
        internal const string HttpDeviceCredentialHeaderName = "X-Device-Authorization";

        // WCF trace source
        internal const string WcfTraceSourceName = "SanteDB.Core.HttpRest";

        // Panic error code
        internal const string GeneralPanicErrorCode = "01189998819991197253";
        // General panic error text
        internal const string GeneralPanicErrorText = "0118 999 881 999 119 7253 - FATAL ERROR: {0}";

        /// <summary>
        /// Service trace source name
        /// </summary>
        public const string ServiceTraceSourceName = "SanteDB.Core";
        /// <summary>
        /// Data source name
        /// </summary>
        public const string DataTraceSourceName = ServiceTraceSourceName + ".Data";
    }
}
