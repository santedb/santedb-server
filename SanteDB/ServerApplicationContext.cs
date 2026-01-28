/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core;
using SanteDB.Core.Data;
using SanteDB.Core.Model.EntityLoader;
using SanteDB.Core.Services.Impl;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB
{
    /// <summary>
    /// Server based application context
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ServerApplicationContext : SanteDBContextBase
    {

        /// <summary>
        /// Server application context
        /// </summary>
        public ServerApplicationContext(string configurationFile) : base(SanteDBHostType.Server, new FileConfigurationService(configurationFile, true))
        {
            EntitySource.Current = new EntitySource(new PersistenceEntitySource());
        }

    }
}