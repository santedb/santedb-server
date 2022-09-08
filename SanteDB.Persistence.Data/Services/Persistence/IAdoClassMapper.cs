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
 * Date: 2022-9-7
 */
using SanteDB.OrmLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Persistence.Data.Services.Persistence
{
    /// <summary>
    /// Represents a persistence service that has subclass conversion
    /// </summary>
    /// <remarks>
    /// When calling a get on a class such as Entity or Material, the classification code dictates the actual class
    /// type which is stored. This interface is implemented by any derived interface so that the conversion process
    /// can reach out to the higher level handler and "convert" it to a desired type.
    /// </remarks>
    internal interface IAdoClassMapper : IAdoPersistenceProvider
    {

        /// <summary>
        /// Maps the <paramref name="dbModel"/> to an appropriate entity model
        /// </summary>
        /// <param name="context">The context on which this is being converted</param>
        /// <param name="dbModel">The existing database model data loaded from the store</param>
        /// <param name="referenceObjects">Additional reference objects which have also been loaded from the database</param>
        /// <returns>The converted object</returns>
        object MapToModelInstanceEx(DataContext context, object dbModel, params object[] referenceObjects);

    }
}
