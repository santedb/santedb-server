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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Diagnostics.OpenProject.Api
{
    /// <summary>
    /// Work package
    /// </summary>
    [JsonObject]
    public class WorkPackage
    {

        /// <summary>
        /// Work package ctor
        /// </summary>
        public WorkPackage()
        {
            this.Attachments = new List<Attachment>();
        }

        /// <summary>
        /// Gets the identifier
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the subject of the object
        /// </summary>
        [JsonProperty("subject")]
        public String Subject { get; set; }

        /// <summary>
        /// Gets or sets the formatted text description
        /// </summary>
        [JsonProperty("description")]
        public FormattedText Description { get; set; }

        /// <summary>
        /// Gets or sets the time that this package was created
        /// </summary>
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the attachments
        /// </summary>
        [JsonIgnore]
        public List<Attachment> Attachments { get; set; }
        
        /// <summary>
        /// Gets the type of item
        /// </summary>
        [JsonProperty("type")]
        public int Type { get; set; }
    }
}
