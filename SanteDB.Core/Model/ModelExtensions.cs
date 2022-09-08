/*
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
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Map;
using System;
using System.Collections.Generic;

namespace SanteDB.Server.Core.Model
{
    /// <summary>
    /// Model extensions
    /// </summary>
    public static class ModelExtensions
    {
        
        /// <summary>
        /// Validates that this object has a target entity
        /// </summary>
        public static IEnumerable<ValidationResultDetail> Validate<TSourceType>(this VersionedAssociation<TSourceType> me) where TSourceType : VersionedEntityData<TSourceType>, new()
        {
            var validResults = new List<ValidationResultDetail>();
            if (me.SourceEntityKey == Guid.Empty)
                validResults.Add(new ValidationResultDetail(ResultDetailType.Error, String.Format("({0}).{1} required", me.GetType().Name, "SourceEntityKey"), null, null));
            return validResults;
        }

        /// <summary>
        /// Validate the state of this object
        /// </summary>
        public static IEnumerable<ValidationResultDetail> Validate(this IdentifiedData me)
        {
            return new List<ValidationResultDetail>();
        }
        
    }
}
