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
    /// Represents an attachment
    /// </summary>
    [JsonObject]
    public class Attachment
    {
        /// <summary>
        /// Gets or sets the title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the filename
        /// </summary>
        [JsonProperty("fileName")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file size
        /// </summary>
        [JsonProperty("fileSize")]
        public int FileSize { get; set; }

        /// <summary>
        /// Gets or sets the file content type
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        [JsonProperty("description")]
        public FormattedText Description { get; set; }
    }
}