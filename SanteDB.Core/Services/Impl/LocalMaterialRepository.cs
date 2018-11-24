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
 * Date: 2018-6-22
 */
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MARC.HI.EHRS.SVC.Core.Data;
using SanteDB.Core.Exceptions;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local material persistence service
    /// </summary>
    public class LocalMaterialRepository : GenericLocalEntityRepository<Material>
    {
        protected override string QueryPolicy => PermissionPolicyIdentifiers.QueryMaterials;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMaterials;
        protected override string WritePolicy => PermissionPolicyIdentifiers.WriteMaterials;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.DeleteMaterials;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.WriteMaterials;
    }

   
}