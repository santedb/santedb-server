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
using NUnit.Framework;
using SanteDB.Caching.Memory;
using SanteDB.Core;
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Core.TestFramework;
using SanteDB.Persistence.Data.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SanteDB.Persistence.Data.Test
{
    /// <summary>
    /// An abstract test fixture which handles the initialization of data tests
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class DataPersistenceTest : DataTest
    {
        // Application ID service
        protected IServiceManager m_serviceManager;

        // Locale service
        protected ILocalizationService m_localizationService;

        // Ignore property
        private string[] IGNORE = new string[] {
            "Type",
            "PreviousVersionKey",
            "Tag",
            "VersionSequence",
            "ModifiedOn",
            "CreationTimeXml",
            "UpdatedTimeXml",
            "ObsoletionTimeXml",
            "VersionKey",
            "Key",
            "CreationTime",
            "CreatedByKey",
            "UpdatedByKey",
            "UpdatedTime",
            "ObsoletedByKey",
            "ObsoletionTime" ,
            "StatusConceptKey",
            "BatchOperation",
            "IsHeadVersion"
        };

        /// <summary>
        /// Setup the test
        /// </summary>
        [OneTimeSetUp]
        public virtual void Setup()
        {
            // Force load of the DLL
            this.m_localizationService = new TestLocalizationService();
            var p = FirebirdSql.Data.FirebirdClient.FbCharset.Ascii;
            TestApplicationContext.TestAssembly = typeof(DataPersistenceTest).Assembly;
            TestApplicationContext.Initialize(TestContext.CurrentContext.TestDirectory);
            this.m_serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
            this.m_serviceManager.AddServiceProvider(typeof(TestQueryPersistenceService));
            this.m_serviceManager.AddServiceProvider(typeof(AdoApplicationIdentityProvider));
            this.m_serviceManager.AddServiceProvider(typeof(AdoDeviceIdentityProvider));
            this.m_serviceManager.AddServiceProvider(typeof(AdoIdentityProvider));
            this.m_serviceManager.AddServiceProvider(typeof(AdoSessionProvider));
            this.m_serviceManager.AddServiceProvider(typeof(AdoPersistenceService));

        }

        /// <summary>
        /// Test that the insert works
        /// </summary>
        protected TData TestInsert<TData>(TData objectToTest) where TData : BaseEntityData
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TData>>();
            Assert.IsNotNull(persistenceService);

            var afterInsert = persistenceService.Insert(objectToTest, TransactionMode.Commit, AuthenticationContext.Current.Principal);

            // Assert core properties are inserted
            Assert.IsNotNull(afterInsert.Key);
            Assert.IsNotNull(afterInsert.CreatedByKey);
            Assert.IsNotNull(afterInsert.CreationTime);

            this.AssertEqual(objectToTest, afterInsert);

            return afterInsert;
        }

        /// <summary>
        /// Assert object<paramref name="objectToTest"/> and <paramref name="objectToTest"/> contain same data
        /// </summary>
        private void AssertEqual<TData>(TData objectToTest, TData afterAction)
        {
            // Assert equality
            foreach (var pi in typeof(TData).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (IGNORE.Contains(pi.Name)) continue;
                object source = pi.GetValue(objectToTest),
                    after = pi.GetValue(afterAction);
                if (source != null && !(source is IdentifiedData || source is IList))
                {
                    Assert.AreEqual(source, after, $"After insert for {pi.Name} value of {after} does not match source {source}");
                }
            }
        }

        /// <summary>
        /// Tests the query operation
        /// </summary>
        protected IEnumerable<TData> TestQuery<TData>(Expression<Func<TData, bool>> queryFilter, int? expectedResults) where TData : BaseEntityData
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TData>>();
            Assert.IsNotNull(persistenceService);
            ApplicationServiceContext.Current.GetService<IDataCachingService>().Clear();

            var queryResults = persistenceService.Query(queryFilter, AuthenticationContext.Current.Principal);
            if (expectedResults.HasValue)
            {
                Assert.AreEqual(expectedResults, queryResults.Count());
            }
            return queryResults;
        }

        /// <summary>
        /// Tests the persistence layer updates <paramref name="objectToTest"/> properly by running each of the <paramref name="updateInstructions"/>
        /// </summary>
        protected TData TestUpdate<TData>(TData objectToTest, params Func<TData, TData>[] updateInstructions) where TData : BaseEntityData
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TData>>();
            Assert.IsNotNull(persistenceService);

            TData afterUpdate = objectToTest;

            // Now we want to update the afterUpdate according to the update tests and then assert
            foreach (var instr in updateInstructions)
            {
                var iteration = instr(afterUpdate);
                afterUpdate = persistenceService.Update(iteration, TransactionMode.Commit, AuthenticationContext.Current.Principal);

                // Assert core properties are inserted
                Assert.IsNotNull(afterUpdate.Key);
                Assert.IsNotNull(afterUpdate.CreatedByKey);
                Assert.IsNotNull(afterUpdate.CreationTime);
                if (afterUpdate is NonVersionedEntityData nve)
                {
                    Assert.IsNotNull(nve.UpdatedTime);
                    Assert.IsNotNull(nve.UpdatedByKey);
                }

                this.AssertEqual(iteration, afterUpdate);
            }

            return afterUpdate;
        }

        /// <summary>
        /// Test that the data is obsoleted
        /// </summary>
        protected TData TestDelete<TData>(TData objectToTest, DeleteMode deleteMode) where TData : BaseEntityData
        {
            var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<TData>>();
            Assert.IsNotNull(persistenceService);

            using (DataPersistenceControlContext.Create(deleteMode))
            {
                var afterObsolete = persistenceService.Delete(objectToTest.Key.Value, TransactionMode.Commit, AuthenticationContext.Current.Principal);

                // Assert core properties are inserted
                Assert.IsNotNull(afterObsolete.Key);
                Assert.IsNotNull(afterObsolete.CreatedByKey);
                Assert.IsNotNull(afterObsolete.CreationTime);

                if (deleteMode == DeleteMode.LogicalDelete)
                {
                    Assert.IsNotNull(afterObsolete.ObsoletedByKey);
                    Assert.IsNotNull(afterObsolete.ObsoletionTime);
                }
                //this.AssertEqual(objectToTest, afterObsolete);
                return afterObsolete;
            }
        }
    }
}