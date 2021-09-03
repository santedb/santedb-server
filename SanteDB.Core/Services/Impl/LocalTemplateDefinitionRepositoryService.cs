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
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using System;
using System.Linq;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a local metadata repository service
    /// </summary>
    public class LocalTemplateDefinitionRepositoryService :
        GenericLocalMetadataRepository<TemplateDefinition>,
        ITemplateDefinitionRepositoryService
    {

        /// <summary>
        /// Get a template definition by mnemonic
        /// </summary>
        public TemplateDefinition GetTemplateDefinition(string mnemonic)
        {
            int t = 0;
            return base.Find(o => o.Mnemonic == mnemonic, 0, 1, out t, Guid.Empty).FirstOrDefault();
        }
        
    }
}