﻿/*
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
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Security;
using System;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Generic local security repository
    /// </summary>
    public abstract class GenericLocalSecurityRepository<TSecurityEntity> : GenericLocalMetadataRepository<TSecurityEntity>
        where TSecurityEntity : IdentifiedData
    {

        /// <summary>
        /// Create new local security repository
        /// </summary>
        public GenericLocalSecurityRepository(IPrivacyEnforcementService privacyService = null) : base(privacyService)
        {
        }

        /// <summary>
        /// Insert the object
        /// </summary>
        public override TSecurityEntity Insert(TSecurityEntity data)
        {
            var retVal = base.Insert(data);
            return retVal;
        }

        /// <summary>
        /// Save the object
        /// </summary>
        public override TSecurityEntity Save(TSecurityEntity data)
        {
            var retVal = base.Save(data);
            return retVal;
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override TSecurityEntity Obsolete(Guid key)
        {
            var retVal = base.Obsolete(key);
            return retVal;
        }
    }
}
