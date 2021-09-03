﻿/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using System;

namespace SanteDB.Persistence.Auditing.ADO.Services
{
    /// <summary>
    /// Audit constants
    /// </summary>
    internal class AuditConstants
    {

        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string ConfigurationSectionName = "santedb.persistence.auditing.ado";

        /// <summary>
        /// Trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Auditing.ADO";

        /// <summary>
        /// The audit is new and has not been reviewed
        /// </summary>
        public readonly Guid StatusNew = Guid.Parse("21B03AD0-70FB-4BEE-A6BC-BDDFAC6BF4A4");

        /// <summary>
        /// The audit has been reviewed by a person
        /// </summary>
        public readonly Guid StatusReviewed = Guid.Parse("31B03AD0-70FB-4BEE-A6BC-BDDFAC6BF4A4");

        /// <summary>
        /// The audit has been obsoleted
        /// </summary>
        public readonly Guid StatusObsolete = Guid.Parse("41B03AD0-70FB-4BEE-A6BC-BDDFAC6BF4A4");

    }
}