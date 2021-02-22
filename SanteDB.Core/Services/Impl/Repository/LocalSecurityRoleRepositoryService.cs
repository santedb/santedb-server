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
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a local security role service
    /// </summary>
    public class LocalSecurityRoleRepositoryService : GenericLocalSecurityRepository<SecurityRole>
    {
        protected override string WritePolicy => PermissionPolicyIdentifiers.CreateRoles;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.AlterRoles;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.AlterRoles;


    }
}