/*
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
using SanteDB.Core.Model.Map;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a persistence settings provider which are used by the persistence services to 
    /// load mappers / configuration etc.
    /// </summary>
    public interface IAdoPersistenceSettingsProvider
    {

        /// <summary>
        /// Get configuration
        /// </summary>
        AdoPersistenceConfigurationSection GetConfiguration();

        /// <summary>
        /// Gets the mode mapper
        /// </summary>
        /// <returns></returns>
        ModelMapper GetMapper();

        /// <summary>
        /// Get query builder
        /// </summary>
        /// <returns></returns>
        QueryBuilder GetQueryBuilder();

        /// <summary>
        /// Get Persister
        /// </summary>
        /// <param name="tDomain"></param>
        /// <returns></returns>
        IAdoPersistenceService GetPersister(Type tDomain);
    }
}
