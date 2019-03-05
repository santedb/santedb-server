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
 * Date: 2019-3-5
 */
using Newtonsoft.Json;

namespace SanteDB.Persistence.Diagnostics.OpenProject.Api
{
    /// <summary>
    /// Formatted text
    /// </summary>
    [JsonObject()]
    public class FormattedText
    {
        /// <summary>
        /// Formated text ctor
        /// </summary>
        public FormattedText()
        {
            this.Format = "markdown";
        }

        /// <summary>
        /// Gets or sets the format
        /// </summary>
        [JsonProperty("format")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the raw format
        /// </summary>
        [JsonProperty("raw")]
        public string Raw { get; set; }

    }
}