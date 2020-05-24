/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2019-11-27
 */
namespace SanteDB.Persistence.Data.ADO
{
    /// <summary>
    /// ADO data constants to be used in the ADO provider
    /// </summary>
    public static class AdoDataConstants
    {

        /// <summary>
        /// Represents the trace source name
        /// </summary>
        public const string TraceSourceName = "SanteDB.Persistence.Data.ADO";
        /// <summary>
        /// Identity trace source name
        /// </summary>
        public const string IdentityTraceSourceName = "SanteDB.Persistence.Data.ADO.Identity";

        /// <summary>
        /// Represents the configuration section name
        /// </summary>
        public const string ConfigurationSectionName = "santedb.persistence.data.ado";

        /// <summary>
        /// Refresh secret claim
        /// </summary>
        public const string RefreshSecretClaimType = "http://santedb.org/claims/sec-ado/refreshSecret";
        /// <summary>
        /// The refresh secret
        /// </summary>
        public const string RefreshExpiryClaimType = "http://santedb.org/claims/sec-ado/refreshExpiry";
        /// <summary>
        /// The map resource file name
        /// </summary>
        public const string MapResourceName = "SanteDB.Persistence.Data.ADO.Data.Map.ModelMap.xml";
    }
}
