﻿/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Persistence.Diagnostics.Jira.Model
{
    /// <summary>
    /// Represents a JIRA issue response
    /// </summary>
    [JsonObject]
    [ExcludeFromCodeCoverage]
    public class JiraIssueResponse
    {

        /// <summary>
        /// Gets or sets the id
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the key
        /// </summary>
        [JsonProperty("key")]
        public String Key { get; set; }

        /// <summary>
        /// Self link
        /// </summary>
        [JsonProperty("self")]
        public String Self { get; set; }

    }
}
