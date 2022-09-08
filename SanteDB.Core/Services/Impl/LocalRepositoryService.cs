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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Registers the <see cref="IRepositoryService"/> instances with the core application context and provides a 
    /// <see cref="IServiceFactory"/> implementation to construct repository services.
    /// </summary>
    /// <remarks>
    /// <para>The instances of <see cref="IRepositoryService"/> which this service constructs contact directly with the 
    /// equivalent <see cref="IDataPersistenceService"/> for each object. The repository layers add business process
    /// logic for calling <see cref="IBusinessRulesService"/>, <see cref="IPrivacyEnforcementService"/>, and others as 
    /// necessary to ensure secure and safe access to the underlying data repositories. All requests to any <see cref="IRepositoryService"/>
    /// constructed by this service use the <see cref="AuthenticationContext"/> to establish "who" is performing the action.</para>
    /// </remarks>
    [ServiceProvider("Local (database) repository service", Dependencies = new Type[] { typeof(IDataPersistenceService) })]
    [Obsolete("Use SanteDB.Core.Services.Impl.Repository.LocalRepositoryFactory", true)]
    public class LocalRepositoryService : SanteDB.Core.Services.Impl.Repository.LocalRepositoryFactory
    {
        public LocalRepositoryService(IServiceManager serviceManager) : base(serviceManager)
        {
        }
    }
}