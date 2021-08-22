using NUnit.Framework;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;

namespace SanteDB.Persistence.Data.ADO.Tests
{
    /// <summary>
    /// Persistence test
    /// </summary>
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
