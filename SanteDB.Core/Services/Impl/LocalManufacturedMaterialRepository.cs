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
 * Date: 2019-1-22
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// Local material persistence service
    /// </summary>
    public class LocalManufacturedMaterialRepository : GenericLocalNullifiedRepository<ManufacturedMaterial>
    {
        protected override string QueryPolicy => PermissionPolicyIdentifiers.QueryMaterials;
        protected override string ReadPolicy => PermissionPolicyIdentifiers.ReadMaterials;
        protected override string WritePolicy => PermissionPolicyIdentifiers.WriteMaterials;
        protected override string DeletePolicy => PermissionPolicyIdentifiers.DeleteMaterials;
        protected override string AlterPolicy => PermissionPolicyIdentifiers.WriteMaterials;
    }
}
