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

namespace SanteDB.Core.Security
{
    /// <summary>
    /// Data policy identifiers
    /// </summary>
    public static class DataPolicyIdentifiers
    {
        /// <summary>
        /// Represents restricted information which is only permitted for those physians directly assigned to the patient
        /// </summary>
        public const string RestrictedInformation = "1.3.6.1.4.1.33349.3.1.5.9.3";
    }
}
