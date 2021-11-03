/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Tests
{
    /// <summary>
    /// Persistence test
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class PersistenceTest<TModel> : DataTest where TModel : IdentifiedData
    {

        /// <summary>
        /// Test the insertion of a valid security user
        /// </summary>
        public TModel DoTestInsert(TModel objectUnderTest, IPrincipal authContext = null)
        {

            // Auth context
            if (authContext == null)
                authContext = AuthenticationContext.AnonymousPrincipal;

            // Store user
            IDataPersistenceService<TModel> persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TModel>>();
            Assert.IsNotNull(persistenceService);

            var objectAfterTest = persistenceService.Insert(objectUnderTest, TransactionMode.Commit, authContext);
            // Key should be set
            Assert.AreNotEqual(Guid.Empty, objectAfterTest.Key);

            // Verify
            objectAfterTest = persistenceService.Get(objectAfterTest.Key.Value, (objectAfterTest as IVersionedEntity)?.VersionKey, false, authContext);
            if(objectAfterTest is BaseEntityData)
                Assert.AreNotEqual(default(DateTimeOffset), (objectAfterTest as BaseEntityData).CreationTime);

            return objectAfterTest;
        }

        /// <summary>
        /// Do a test step for an update
        /// </summary>
        public TModel DoTestUpdate(TModel objectUnderTest, String propertyToChange, IPrincipal authContext = null)
        {

            // Auth context
            if (authContext == null)
                authContext = AuthenticationContext.AnonymousPrincipal;

            // Store user
            IDataPersistenceService<TModel> persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TModel>>();
            Assert.IsNotNull(persistenceService);

            // Update the user
            var objectAfterInsert = persistenceService.Insert(objectUnderTest, TransactionMode.Commit, authContext);

            // Update
            var propertyInfo = typeof(TModel).GetProperty(propertyToChange);
            object originalValue = propertyInfo.GetValue(objectUnderTest);

            if (propertyInfo.PropertyType == typeof(String))
                propertyInfo.SetValue(objectAfterInsert, "NEW_VALUE");
            else if (propertyInfo.PropertyType == typeof(Nullable<DateTimeOffset>) ||
                propertyInfo.PropertyType == typeof(DateTimeOffset))
                propertyInfo.SetValue(objectAfterInsert, DateTimeOffset.MaxValue);
            else if (propertyInfo.PropertyType == typeof(Boolean) ||
                propertyInfo.PropertyType == typeof(Nullable<Boolean>))
                propertyInfo.SetValue(objectAfterInsert, true);

            var objectAfterUpdate = persistenceService.Update(objectAfterInsert, TransactionMode.Commit, authContext);
            Assert.AreEqual(objectAfterInsert.Key, objectAfterUpdate.Key);
            objectAfterUpdate = persistenceService.Get(objectAfterUpdate.Key.Value, (objectAfterInsert as IVersionedEntity)?.VersionKey, false, authContext);
            // Update attributes should be set
            Assert.AreNotEqual(originalValue, propertyInfo.GetValue(objectAfterUpdate));
            Assert.AreEqual(objectAfterInsert.Key, objectAfterUpdate.Key);

            return objectAfterUpdate;
        }

        /// <summary>
        /// Perform a query
        /// </summary>
        public IEnumerable<TModel> DoTestQuery(Expression<Func<TModel, bool>> predicate, Guid? knownResultKey, IPrincipal authContext = null)
        {

            // Auth context
            if (authContext == null)
                authContext = AuthenticationContext.AnonymousPrincipal;

            IDataPersistenceService<TModel> persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TModel>>();
            Assert.IsNotNull(persistenceService);

            // Perform query
            var results = persistenceService.Query(predicate, authContext);

            // Look for the known key
            Assert.IsTrue(results.Any(p => p.Key == knownResultKey), "Result doesn't contain known key");

            return results;
        }

    }
}
