/*
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
 * Date: 2018-9-25
 */
using RestSrvr.Attributes;
using System.IO;

namespace SanteDB.Tools.DataSandbox.Wcf
{
    /// <summary>
    /// Query tool contract
    /// </summary>
    [ServiceContract]
    public interface IDataSandboxTool
    {

        /// <summary>
        /// Create dataset 
        /// </summary>
        [RestInvoke(Method = "POST", UriTemplate = "/dataset")]
        Stream CreateDataset(Stream datasetSource);

        /// <summary>
        /// Get static content 
        /// </summary>
        /// <param name="content">The content to retrieve</param>
        /// <returns>The static content</returns>
        [RestInvoke(Method = "GET", UriTemplate = "/{*content}")]
        Stream StaticContent(string content);

    }
}
